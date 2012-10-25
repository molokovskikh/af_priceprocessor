using System;
using System.Collections;
using System.Data;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

// Read an entire standard DBF file into a DataTable
public class ParseDBF
{
	// This is the file header for a DBF. We do this special layout with everything
	// packed so we can read straight from disk into the structure to populate it
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
	private struct DBFHeader
	{
		public byte version;
		public byte updateYear;
		public byte updateMonth;
		public byte updateDay;
		public Int32 numRecords;
		public Int16 headerLen;
		public Int16 recordLen;
		public Int16 reserved1;
		public byte incompleteTrans;
		public byte encryptionFlag;
		public Int32 reserved2;
		public Int64 reserved3;
		public byte MDX;
		public byte language;
		public Int16 reserved4;
	}

	// This is the field descriptor structure. There will be one of these for each column in the table.
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
	private struct FieldDescriptor
	{
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 11)] public string fieldName;
		public char fieldType;
		public Int32 address;
		public byte fieldLen;
		public byte count;
		public Int16 reserved1;
		public byte workArea;
		public Int16 reserved2;
		public byte flag;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)] public byte[] reserved3;
		public byte indexFlag;
	}

	// Read an entire standard DBF file into a DataTable
	public static DataTable ReadDBF(string dbfFile)
	{
		DataTable dt = new DataTable();

		// If there isn't even a file, just return an empty DataTable
		if ((false == File.Exists(dbfFile))) {
			return dt;
		}

		BinaryReader br = null;
		try {
			// Read the header into a buffer
			br = new BinaryReader(File.OpenRead(dbfFile));
			byte[] buffer = br.ReadBytes(Marshal.SizeOf(typeof(DBFHeader)));

			// Marshall the header into a DBFHeader structure
			GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
			DBFHeader header = (DBFHeader)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DBFHeader));
			handle.Free();

			// Read in all the field descriptors. Per the spec, 13 (0D) marks the end of the field descriptors
			ArrayList fields = new ArrayList();
			while ((13 != br.PeekChar())) {
				buffer = br.ReadBytes(Marshal.SizeOf(typeof(FieldDescriptor)));
				handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
				fields.Add((FieldDescriptor)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(FieldDescriptor)));
				handle.Free();
			}

			// Create the columns in our new DataTable
			DataColumn col = null;
			foreach (FieldDescriptor field in fields) {
				switch (field.fieldType) {
					case 'N':
						col = new DataColumn(field.fieldName, typeof(Int32));
						break;
					case 'C':
						col = new DataColumn(field.fieldName, typeof(string));
						break;
					case 'D':
						col = new DataColumn(field.fieldName, typeof(DateTime));
						break;
					case 'L':
						col = new DataColumn(field.fieldName, typeof(bool));
						break;
				}
				dt.Columns.Add(col);
			}

			// Skip past the end of the header. 
			((FileStream)br.BaseStream).Seek(header.headerLen + 1, SeekOrigin.Begin);

			// Declare all our locals here outside the loops
			BinaryReader recReader;
			string number;
			string year;
			string month;
			string day;
			DataRow row;

			// Read in all the records
			for (int counter = 0; counter <= header.numRecords - 1; counter++) {
				// First we'll read the entire record into a buffer and then read each field from the buffer
				// This helps account for any extra space at the end of each record and probably performs better
				buffer = br.ReadBytes(header.recordLen);
				recReader = new BinaryReader(new MemoryStream(buffer));

				// Loop through each field in a record
				row = dt.NewRow();
				foreach (FieldDescriptor field in fields) {
					switch (field.fieldType) {
						case 'N': // Number
							// We'll use a try/catch here in case it isn't a valid number
							number = Encoding.ASCII.GetString(recReader.ReadBytes(field.fieldLen));
							try {
								row[field.fieldName] = Int32.Parse(number);
							}
							catch {
								row[field.fieldName] = 0;
							}

							break;

						case 'C': // String
							row[field.fieldName] = Encoding.ASCII.GetString(recReader.ReadBytes(field.fieldLen));
							break;

						case 'D': // Date (YYYYMMDD)
							year = Encoding.ASCII.GetString(recReader.ReadBytes(4));
							month = Encoding.ASCII.GetString(recReader.ReadBytes(2));
							day = Encoding.ASCII.GetString(recReader.ReadBytes(2));
							row[field.fieldName] = System.DBNull.Value;
							try {
								if ((Int32.Parse(year) > 1900)) {
									row[field.fieldName] = new DateTime(Int32.Parse(year), Int32.Parse(month), Int32.Parse(day));
								}
							}
							catch {
							}

							break;

						case 'L': // Boolean (Y/N)
							if ('Y' == recReader.ReadByte()) {
								row[field.fieldName] = true;
							}
							else {
								row[field.fieldName] = false;
							}

							break;
					}
				}

				recReader.Close();
				dt.Rows.Add(row);
			}
		}
		catch {
			throw;
		}
		finally {
			if (null != br) {
				br.Close();
			}
		}

		return dt;
	}
}