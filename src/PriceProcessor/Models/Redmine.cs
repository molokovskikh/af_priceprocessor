﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml.Serialization;
using Common.MySql;
using Common.Tools;
using Dapper;
using Inforoom.PriceProcessor.Helpers;
using Inforoom.PriceProcessor.Waybills.Models;
using log4net;
using MySql.Data.MySqlClient;
using NPOI.HPSF;

namespace Inforoom.PriceProcessor.Models
{
    public class Redmine
    {
        private static readonly ILog _InfoLog = LogManager.GetLogger("InfoLog");

        /// <summary>
        /// информация о передоваемом на Redmine файле
        /// </summary>
        public class RedmineFile
        {
            public string Token { get; set; }
            public string Filename { get; set; }
            public string Content_type { get; set; }
            public object Json { get; set; }
        }

        //проверка существующей задачи на Redmine
        private static bool FileAlreadyInIssue(string project, string hash)
        {
            var result = MetadataOfLog.GetMetaFromDataBase(project, hash);
            if (result.HasValue) {
                _InfoLog.Info(
                    $"По неразобранной накладной уже была создана задача {result.Value}");
                return true;
            }
            return false;
        }

        //Создание задачи с файлом для Redmine (в сообщении задачи метаданные логов по документа в формате json)
	    public static void CreateIssueForLog(ref List<MetadataOfLog> metaList, DocumentReceiveLog documentLog)
	    {
		    if (documentLog?.Address?.Client?.RedmineNotificationForUnresolved != true) {
			    return;
		    }

		    var priority = false;

		    SessionHelper.WithSession(s => {
			    priority = s.Connection.Query<int>(@"
SELECT Count(*)  FROM usersettings.RetClientsSet as rc
LEFT JOIN customers.PromotionMembers as pm ON pm.ClientId = rc.ClientCode
WHERE
rc.ClientCode = @clientId
AND (pm.ClientId IS NOT NULL OR rc.IsStockEnabled = 1 )
AND rc.InvisibleOnFirm <> 2
", new {@clientId = documentLog.Address.Client.Id}).FirstOrDefault() > 0;

		    });

		    //создаем задачу на Redmine, прикрепляя файлы
		    if (metaList.All(s => s.Hash != new MetadataOfLog(documentLog).Hash)) {
			    var redmineText =
				    $"Не {(documentLog.DocumentType == DocType.Waybill ? "разобрана накладная" : "разобран отказ")}. клиент: {documentLog?.ClientCode?.ToString() ?? ""}, поставщик: {documentLog?.Supplier?.Name} ({documentLog?.Supplier?.Id})";
			    var newMeta = CreateIssueWithAFile(redmineText, documentLog, priority);
			    if (newMeta != null) {
				    metaList.Add(newMeta);
			    }
		    }
	    }

        //Создание задачи с файлом для Redmine (в сообщении задачи метаданные логов по документа в формате json)
	    public static MetadataOfLog CreateIssueWithAFile(string subject, DocumentReceiveLog log, bool hasPriority)
	    {
		    RedmineFile fileOnRedmine = null;
		    var fi = new FileInfo(log.GetFileName());
		    if (!hasPriority && fi.Extension.ToLower() != ".dbf") {
			    return null;
		    }

		    var redmineProject = hasPriority
			    ? Settings.Default.RedmineProjectForWaybillIssueWithPriority
					: Settings.Default.RedmineProjectForWaybillIssue;
		    var redmineUser = !hasPriority || fi.Extension.ToLower() == ".dbf"
			    ? Settings.Default.RedmineAssignedTo
			    : Settings.Default.RedmineAssignedToWithPriority;

		    var bufferF = File.ReadAllBytes(fi.FullName);
		    var currentMeta = new MetadataOfLog(log);
#if DEBUG
		    //для тестов
		    if (!FileAlreadyInIssue(redmineProject, currentMeta.Hash)) {
			    var debugHash = log.FileName.GetHashCode().ToString().Replace("-", "");
			    var token = debugHash;
#else
            if (!FileAlreadyInIssue(redmineProject, currentMeta.Hash))
            {
                var token =
                    UploadFileToRedmine(
                        string.Format(Settings.Default.RedmineUrlFileUpload, Settings.Default.RedmineKeyForWaybillIssue),
                        bufferF);
#endif
			    if (token != string.Empty) {
				    fileOnRedmine = new RedmineFile {
					    Token = token,
					    Content_type = "application/binary",
					    Filename = fi.Name,
					    Json =
						    $"Служебная информация по {(log.DocumentType == DocType.Waybill ? "накладной" : "отказу")}: {currentMeta.Hash}"
				    };
				    //возвращаем метаданные только, если задача создана
				    return
					    CreateIssue(
						    string.Format(Settings.Default.RedmineUrl, redmineProject,
							    Settings.Default.RedmineKeyForWaybillIssue), subject, fileOnRedmine.Json, redmineUser
						    , new List<RedmineFile> {fileOnRedmine})
						    ? currentMeta
						    : null;
			    }
		    }
		    return null;
	    }

        /// <summary>
        /// Загрузка файла на Redmine
        /// </summary>
        /// <param name="url">путь к Redmine для загрузки файлов</param>
        /// <param name="file">путь к файлу</param>
        /// <param name="login">[авторизация на Redmine].логин</param>
        /// <param name="password">[авторизация на Redmine].пароль</param>
        /// <returns></returns>
        public static string UploadFileToRedmine(string url, byte[] file)
        {
            using (var client = new HttpClient()) {
                var bt = new ByteArrayContent(file);
                bt.Headers.Add("Content-Type", "application/octet-stream");
                using (var result = client.PostAsync(url, bt).Result) {
                    if (result.StatusCode == HttpStatusCode.Created) {
                        var xres = XElement.Parse(result.Content.ReadAsStringAsync().Result);
                        return xres.Value;
                    }
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Создание задачи на Redmine
        /// </summary>
        /// <param name="url">путь к Redmine для создания задач</param>
        /// <param name="subject">Заголовок задачи</param>
        /// <param name="body">Создержание задачи</param>
        /// <param name="assignedTo">Исполнитель</param>
        /// <param name="login">[авторизация на Redmine].логин</param>
        /// <param name="password">[авторизация на Redmine].пароль</param>
        /// <param name="files">Список "информации о загруженных на Redmine файлах"</param>
        /// <returns></returns>
        public static bool CreateIssue(string url, string subject, object body, string assignedTo,
            List<RedmineFile> files = null)
        {
#if DEBUG
	        //для тестов
	        using (var sqlConnection = new MySqlConnection(ConnectionHelper.GetConnectionString())) {
		        var project = url.Split('/').Any(s => s.ToLower()
			        .IndexOf(Settings.Default.RedmineProjectForWaybillIssue.ToLower(), StringComparison.Ordinal) != -1)
			        ? Settings.Default.RedmineProjectForWaybillIssue
			        : Settings.Default.RedmineProjectForWaybillIssueWithPriority;
		        sqlConnection.Open();
		        sqlConnection.Query($"INSERT INTO  redmine.issues (description, project_id) VALUES(@ddata, '{project}')",
			        new {ddata = body});
		        sqlConnection.Close();
	        }
	        return true;
#endif

            if (String.IsNullOrEmpty(url))
                return false;
            object data;
            if (files != null || files.Count == 0) {
                data = new {
                    issue = new {
                        subject = subject,
                        description = body,
                        assigned_to_id = assignedTo,
                        uploads =
                            files.Select(
                                s => new {token = s.Token, filename = s.Filename, content_type = s.Content_type})
                                .ToList()
                    }
                };
            } else {
                data = new {
                    issue = new {
                        subject = subject,
                        description = body,
                        assigned_to_id = assignedTo
                    }
                };
            }

            using (var client = new HttpClient()) {
                var result = client.PostAsJsonAsync(url, data).Result;
                if (result.StatusCode == HttpStatusCode.Created) {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Метаданные о логах по обрабатываемым файлам
    /// </summary>
    [Serializable()]
    public class MetadataOfLog
    {
        public MetadataOfLog()
        {
        }

        static byte[] ObjectToByteArray(object obj)
        {
            if (obj == null)
                return null;
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream()) {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Создание метаданных на основе логов
        /// </summary>
        /// <param name="log"></param>
        public MetadataOfLog(DocumentReceiveLog log)
        {
            FileInfo fileInfo = string.IsNullOrEmpty(log.FileName)
                ? null
                : new FileInfo(log.FileName);
            ClientCode = log.ClientCode?.ToString() ?? "";
            Address = log.Address?.Id.ToString() ?? "";
            Supplier = log.Supplier?.Id.ToString() ?? "";
            DocumentType = log.DocumentType.ToString();
            FileExtension = fileInfo?.Extension;
            //получаем хэш от ключа
            using (var sha256Hash = new SHA256Managed()) {
                var hash = sha256Hash.ComputeHash(ObjectToByteArray(this));
                Hash = Convert.ToBase64String(hash);
            }
        }
        public string ClientCode { get; set; }
        public string Address { get; set; }
        public string Supplier { get; set; }
        public string DocumentType { get; set; }
        public string FileExtension { get; set; }
        public string Hash { get; set; }


        public static int? GetMetaFromDataBase(string project, string hash)
        {
            using (var sqlConnection =
                new MySqlConnection(ConnectionHelper.GetConnectionString())) {
                sqlConnection.Open();
                var result =
                    sqlConnection.Query<int?>(
                        $"SELECT id FROM redmine.issues WHERE project_id = '{project}' AND description LIKE '%{hash}%'")
                        .FirstOrDefault();
                sqlConnection.Close();
                if (result.HasValue)
                    return result;
            }
            return null;
        }

        public static int GetMetaFromDataBaseCount(string project, string hash)
        {
            using (var sqlConnection =
                new MySqlConnection(ConnectionHelper.GetConnectionString())) {
                sqlConnection.Open();
                var result =
                    sqlConnection.Query<int>(
                        $"SELECT Count(id)  FROM redmine.issues WHERE project_id = '{project}' AND description LIKE '%{hash}%'")
                        .FirstOrDefault();
                sqlConnection.Close();
                return result;
            }
        }
    }
}