using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Data;

namespace Inforoom.Downloader
{
    public class LANSourceHandler : PathSourceHandler
    {
        public LANSourceHandler(string sourceType)
            : base(sourceType)
        { }

        protected override void GetFileFromSource()
        {
            CurrFileName = String.Empty;
            string PricePath = NormalizeDir((string)dtSources.Rows[0][SourcesTable.colPricePath]);
            try
            {
                string[] ff = Directory.GetFiles(PricePath, (string)dtSources.Rows[0][SourcesTable.colPriceMask]);
                DateTime pdt = ((MySql.Data.Types.MySqlDateTime)dtSources.Rows[0][SourcesTable.colPriceDateTime]).GetDateTime();
                foreach (string fs in ff)
                {
                    if (File.GetLastWriteTime(fs).CompareTo(pdt) > 0)
                    {
                        pdt = File.GetLastWriteTime(fs);
                        string NewFile = DownHandlerPath + Path.GetFileName(fs);
                        try
                        {
                            //TODO: здесь надо Copy изменить на Move
                            File.Copy(fs, NewFile);
                            CurrFileName = NewFile;
                            CurrPriceDate = pdt;
                            break;
                        }
                        catch
                        {
                        }
                    }
                }
            }
            catch{}
        }

        protected override DataRow[] GetLikeSources()
        { 
            return dtSources.Select(String.Format("({0} = '{1}') and ({2} = '{3}')", 
                SourcesTable.colPricePath, dtSources.Rows[0][SourcesTable.colPricePath],
                SourcesTable.colPriceMask, dtSources.Rows[0][SourcesTable.colPriceMask]));
        }

    }
}
