using System.Text;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using GsoReporterProto.V1;
using SharedLibrary.Models;

namespace BlazorLibrary
{
    public static class ReportGenerate
    {
        private static byte[] GetHtml(string TitleName, List<string> ColumnNameList, List<List<string>> ColumnValueList, List<string>? OtherInfo = null, bool? Center = false, string? filtr = null, ReporterFont? Font = null, List<List<string>>? dopTable = null)
        {

            List<string> reports = new()
            {
                "<html><head><META HTTP-EQUIV=\"Content-Type\" CONTENT=\"text/html; charset = UTF-8\"><STYLE>",
                "BODY {font-family:" + (Font?.StrName ?? "Verdana") + "; background-color:white; color:black;} ",
                "Table{ font-size:" + Font?.Size + "pt; font-style:" + (Font?.Style == 0 ? "normal" : "italic") + ";}",
                "THEAD{background-color:#e0e0e0; font-weight: 700;}",
                "TBODY{background-color: #f0f0f0; text-align:" + ((Center ?? false) ? "center" : "left") + ";font-weight: " + Font?.Weight + ";}",
                "@media print{.noprint{ display:none;}}</STYLE>",
                $"<title>{TitleName}</title></head><body><h{6 - Font?.FontSize} align=\"center\">{TitleName}</h{6 - Font?.FontSize}>"
            };

            if (!string.IsNullOrEmpty(filtr))
                reports.Add($"<p style=\"color:red;\">{filtr}</p>");

            if (OtherInfo?.Count > 0)
            {
                foreach (var item in OtherInfo)
                {
                    reports.Add($"<p style=\"font-size:" + Font?.Size + "pt; font-weight: " + Font?.Weight + ";font-style:" + (Font?.Style == 0 ? "normal" : "italic") + ";\">" + item + "</p>");
                }
            }
            reports.Add(@"<script>
                        function ShowHide(idElem, buttonElem) {
                            var elem = document.getElementById(idElem);
                            if (elem) {
                                if (elem.style.display === ""none"") {
                                    elem.style.display = ""table"";
                                    buttonElem.innerHTML = '&#9866;';
                                } else {
                                    elem.style.display = ""none"";
                                    buttonElem.innerHTML = '&#10010;';
                                }
                            }
                        }
                        </script>");

            if (dopTable?.Count > 0)
            {
                reports.Add("<a href='#' class=\"noprint\" onclick='ShowHide(\"dopTable\", this)'>&#9866;</a>");
                reports.Add("<table id=\"dopTable\"  width=\"100%\" cellpadding=\"1\" cellspacing=\"1\">");
                reports.Add("<THEAD>");
                reports.Add("<tr height=\"50\">");

                foreach (var item in dopTable.First())
                {
                    reports.Add($"<th>{item}</th>");
                }

                reports.Add("</tr>");
                reports.Add("</THEAD>");
                foreach (var item in dopTable.Skip(1))
                {
                    reports.Add("<tr>");

                    foreach (var itemValue in item)
                    {
                        reports.Add($"<td>{itemValue}</td>");
                    }

                    reports.Add("</tr>");
                }
                reports.Add("</table>");
                reports.Add("<br/>");
            }



            reports.Add("<a href='#' class=\"noprint\" onclick='ShowHide(\"generalTable\", this)'>&#9866;</a>");

            reports.Add("<table id=\"generalTable\" width=\"100%\" cellpadding=\"1\" cellspacing=\"1\">");
            reports.Add("<THEAD>");
            reports.Add("<tr height=\"50\">");

            if (ColumnNameList != null)
            {

                foreach (var item in ColumnNameList)
                {
                    reports.Add($"<th>{item}</th>");
                }

            }
            reports.Add("</tr>");
            reports.Add("</THEAD>");
            if (ColumnValueList != null)
            {
                foreach (var item in ColumnValueList)
                {
                    reports.Add("<tr>");

                    foreach (var itemValue in item)
                    {
                        reports.Add($"<td>{itemValue}</td>");
                    }

                    reports.Add("</tr>");
                }
            }
            reports.Add("</table></body></html>");


            return Encoding.UTF8.GetBytes(string.Join("", reports));

        }


        /// <summary>
        ///Генерация HTML
        /// </summary>
        /// <param name="RepInfo">инфо отчета</param>
        /// <param name="ColumnList">столбцы и др инфо из базы</param>
        /// <param name="ReportId">Report Id</param>
        /// <returns></returns>        
        public static byte[] GetReportForProto<TData>(ReportInfo RepInfo, List<GetColumnsExItem> ColumnList, int ReportId, List<TData> Model, List<string>? OtherInfo = null, string? filtr = null, List<List<string>>? dopTable = null) where TData : IMessage<TData>
        {
            Dictionary<int, string> ColumnNameList = new Dictionary<int, string>();

            Dictionary<int, string> Column = new();

            switch (ReportId)
            {
                case 1: Column = ColumnSit; break;
                case 2: Column = ColumnAbon; break;
                case 3: Column = ColumnASO; break;
                case 4: Column = ColumnChannel; break;
                case 5: Column = ColumnCU; break;
                case 6: Column = ColumnLog; break;
                case 7: Column = ColumnCUDetali; break;
                case 8: Column = ColumnSZS; break;
                case 9: Column = ColumnHistoryCall; break;
                case 10: Column = ColumnP16; break;
                case 11: Column = ColumnEventLog; break;//event log
            }

            ColumnList = ColumnList.OrderBy(x => x.NColumnId).ToList();
            if (ColumnList.FirstOrDefault(x => x.NColumnId == 3)?.NStatus != 1)
            {
                OtherInfo = null;
            }

            if (ColumnList.FirstOrDefault(x => x.NColumnId == 200)?.NStatus == 1)
            {
                ColumnNameList = ColumnList.Where(x => x.NColumnId >= 4 && x.NColumnId < 99 && x.NStatus == 1).ToDictionary(x => x.NColumnId, x => x.TContrName);
            }
            else
            {
                ColumnNameList = ColumnList.Where(x => x.NColumnId >= 4 && x.NColumnId < 99 && x.NStatus == 1).ToDictionary(x => x.NColumnId, x => x.TName);
            }

            List<List<string>> ColumnValueList = new();
            List<string> ColumnNameList2 = new();
            foreach (var itemProto in Model)
            {
                List<string> child = new();
                foreach (var item in ColumnNameList)
                {
                    if (Column.ContainsKey(item.Key))
                    {
                        var nameColumn = Column[item.Key];

                        FieldDescriptor field = itemProto.Descriptor.FindFieldByName(nameColumn);
                        string valueField = "";

                        if (field != null)
                        {
                            try
                            {
                                if (field.IsRepeated == false)
                                {
                                    if (field.FieldType != FieldType.Message)
                                    {
                                        valueField = field.Accessor.GetValue(itemProto)?.ToString() ?? "";
                                    }
                                    else if (field.MessageType == Timestamp.Descriptor)
                                    {
                                        valueField = ((Timestamp)field.Accessor.GetValue(itemProto))?.ToDateTime().ToLocalTime().ToString() ?? "";
                                    }
                                    else if (field.MessageType == SMDataServiceProto.V1.LabelNameValueFieldList.Descriptor)
                                    {
                                        var labels = ((SMDataServiceProto.V1.LabelNameValueFieldList)field.Accessor.GetValue(itemProto));
                                        if (labels != null && labels.List?.Count(x => !string.IsNullOrEmpty(x.ValueField)) > 0)
                                        {
                                            valueField = string.Join(@"<br/>", labels.List.Where(x => !string.IsNullOrEmpty(x.ValueField)).Select(x => $"{x.NameField}: {x.ValueField}"));
                                        }
                                    }
                                }
                                else
                                {
                                    if (field.FieldType == FieldType.String)
                                    {
                                        valueField = string.Join(@"<br/>", (RepeatedField<string>)field.Accessor.GetValue(itemProto));
                                    }
                                }
                            }
                            catch
                            {
                                Console.WriteLine($"Create report error get value for {nameColumn}");
                            }
                        }
                        child.Add(valueField);
                    }
                }
                ColumnValueList.Add(child);
            }

            if (ReportId == 4)//для состояния каналов (Асо)
            {
                ColumnValueList.ForEach(x =>
                {
                    if (x[14].Split('/').Length > 1)
                    {
                        x[1] = x[14].Split('/')[1];
                        x[14] = x[14].Split('/')[0];
                    }
                });
            }

            bool Center = ColumnList.FirstOrDefault(x => x.NColumnId == 201)?.NStatus == 0 ? false : true;

            return GetHtml(RepInfo.Name, ColumnNameList.Values.ToList(), ColumnValueList, OtherInfo, Center, filtr, new ReporterFont(RepInfo.Font), dopTable);
        }


        public static string CreateHtmlTableForProto<TData>(List<GetColumnsExItem> columnList, int reportId, List<TData> model, string nameTable) where TData : IMessage<TData>
        {
            Dictionary<int, string> ColumnNameList = new Dictionary<int, string>();
            Dictionary<int, string> Column = new();
            switch (reportId)
            {
                case 1: Column = ColumnSit; break;
                case 2: Column = ColumnAbon; break;
                case 3: Column = ColumnASO; break;
                case 4: Column = ColumnChannel; break;
                case 5: Column = ColumnCU; break;
                case 6: Column = ColumnLog; break;
                case 7: Column = ColumnCUDetali; break;
                case 8: Column = ColumnSZS; break;
                case 9: Column = ColumnHistoryCall; break;
                case 10: Column = ColumnP16; break;
                case 11: Column = ColumnEventLog; break;//event log
            }
            columnList = columnList.OrderBy(x => x.NColumnId).ToList();
            if (columnList.FirstOrDefault(x => x.NColumnId == 200)?.NStatus == 1)
            {
                ColumnNameList = columnList.Where(x => x.NColumnId >= 4 && x.NColumnId < 99 && x.NStatus == 1).ToDictionary(x => x.NColumnId, x => x.TContrName);
            }
            else
            {
                ColumnNameList = columnList.Where(x => x.NColumnId >= 4 && x.NColumnId < 99 && x.NStatus == 1).ToDictionary(x => x.NColumnId, x => x.TName);
            }

            List<List<string>> ColumnValueList = new();
            List<string> ColumnNameList2 = new();
            foreach (var itemProto in model)
            {
                List<string> child = new();
                foreach (var item in ColumnNameList)
                {
                    if (Column.ContainsKey(item.Key))
                    {
                        var nameColumn = Column[item.Key];

                        FieldDescriptor field = itemProto.Descriptor.FindFieldByName(nameColumn);
                        string valueField = "";

                        if (field != null)
                        {
                            try
                            {
                                if (field.IsRepeated == false)
                                {
                                    if (field.FieldType != FieldType.Message)
                                    {
                                        valueField = field.Accessor.GetValue(itemProto)?.ToString() ?? "";
                                    }
                                    else if (field.MessageType == Timestamp.Descriptor)
                                    {
                                        valueField = ((Timestamp)field.Accessor.GetValue(itemProto))?.ToDateTime().ToLocalTime().ToString() ?? "";
                                    }
                                    else if (field.MessageType == SMDataServiceProto.V1.LabelNameValueFieldList.Descriptor)
                                    {
                                        var labels = ((SMDataServiceProto.V1.LabelNameValueFieldList)field.Accessor.GetValue(itemProto));
                                        if (labels != null && labels.List?.Count(x => !string.IsNullOrEmpty(x.ValueField)) > 0)
                                        {
                                            valueField = string.Join(@"<br/>", labels.List.Where(x => !string.IsNullOrEmpty(x.ValueField)).Select(x => $"{x.NameField}: {x.ValueField}"));
                                        }
                                    }
                                }
                                else
                                {
                                    if (field.FieldType == FieldType.String)
                                    {
                                        valueField = string.Join(@"<br/>", (RepeatedField<string>)field.Accessor.GetValue(itemProto));
                                    }
                                }
                            }
                            catch
                            {
                                Console.WriteLine($"Create report error get value for {nameColumn}");
                            }
                        }
                        child.Add(valueField);
                    }
                }
                ColumnValueList.Add(child);
            }

            if (reportId == 4)//для состояния каналов (Асо)
            {
                ColumnValueList.ForEach(x =>
                {
                    if (x[14].Split('/').Length > 1)
                    {
                        x[1] = x[14].Split('/')[1];
                        x[14] = x[14].Split('/')[0];
                    }
                });
            }
            return CreateHtmlTable(ColumnNameList?.Values, ColumnValueList, nameTable);

        }


        public static string CreateHtmlTable(IEnumerable<string>? headerList, IEnumerable<IEnumerable<string>>? valueList, string? nameTable = null)
        {
            var idTable = Path.ChangeExtension(Path.GetRandomFileName(), "").TrimEnd('.');
            List<string> reports = new()
            {                
                $"<table id=\"{idTable}\" width=\"100%\" cellpadding=\"1\" cellspacing=\"1\">",
                "<caption style=\"text-align: left;\">",
                $"<a href='#' class=\"noprint\" style=\"text-decoration: none;\" onclick='ShowHide(\"{idTable}\", this)'>&#9866;</a> {nameTable}",
                "</caption>"
            };

            if (headerList?.Count() > 0)
            {
                reports.Add($"<thead forid=\"{idTable}\">");
                reports.Add("<tr height=\"50\">");
                foreach (var item in headerList)
                {
                    reports.Add($"<th>{item}</th>");
                }
                reports.Add("</tr>");
                reports.Add("</thead>");
            }
            if (valueList?.Count() > 0)
            {
                reports.Add($"<tbody forid=\"{idTable}\">");
                foreach (var item in valueList)
                {
                    reports.Add("<tr>");

                    foreach (var itemValue in item)
                    {
                        reports.Add($"<td>{itemValue}</td>");
                    }

                    reports.Add("</tr>");
                }
                reports.Add("</tbody>");
            }
            reports.Add("</table><br/>");
            return string.Join("", reports);
        }

        public static string CreateHtmlSection(string bodyContent, string? nameTable = null)
        {
            var idTable = Path.ChangeExtension(Path.GetRandomFileName(), "").TrimEnd('.');
            List<string> reports = new()
            {
                $"<div id=\"{idTable}\" >",
                $"<a href='#' class=\"noprint\" style=\"text-decoration: none;\" onclick='ShowHide(\"{idTable}\", this)'>&#9866;</a> {nameTable}",
                $"<div class=\"childSection\" forid=\"{idTable}\">",
                bodyContent,
                "</div>",
                "</div><br/>"
            };
            return string.Join("", reports);
        }

        public static byte[] GetHtml(IEnumerable<string> bodyContent, ReportInfo repInfo, bool? Center = false, string? filtr = null)
        {
            var Font = new ReporterFont(repInfo.Font);

            List<string> reports = new()
            {
                "<html>",
                "<head>",
                "<META HTTP-EQUIV=\"Content-Type\" CONTENT=\"text/html; charset = UTF-8\">",
                "<STYLE>",
                "BODY {font-family:" + (Font?.StrName ?? "Verdana") + "; background-color:white; color:black;} ",
                "Table{ font-size:" + Font?.Size + "pt; font-style:" + (Font?.Style == 0 ? "normal" : "italic") + ";}",
                "THEAD{background-color:#e0e0e0; font-weight: 700;}",
                "TBODY{background-color: #f0f0f0; text-align:" + ((Center ?? false) ? "center" : "left") + ";font-weight: " + Font?.Weight + ";}",
                "@media print{.noprint{ display:none;}} .childSection{margin-left:20px;} ",
                "</STYLE>",
                $"<title>{repInfo.Name}</title>",
                "</head>",
                $"<body>",
                $"<h{6 - Font?.FontSize} align=\"center\">{repInfo.Name}</h{6 - Font?.FontSize}>"
            };

            if (!string.IsNullOrEmpty(filtr))
                reports.Add($"<p style=\"color:red;\">{filtr}</p>");

            reports.Add(@"<script>
                        function ShowHide(idElem, buttonElem) {
                            var elem = document.getElementById(idElem);
                            var arrayElem = elem.querySelectorAll('[forid=""' + idElem + '""]');
                            if (arrayElem) {
                                arrayElem.forEach(x => {
                                    if (x.style.display === 'none') {
                                        x.removeAttribute('style');
                                        elem.classList.remove('noprint');
                                        buttonElem.innerHTML = '&#9866;';
                                    } else {
                                        x.style.display = 'none';
                                        elem.classList.add('noprint');
                                        buttonElem.innerHTML = '&#10010;';
                                    }
                                });        
                            }
                        }
                        </script>");
            reports.AddRange(bodyContent);
            reports.Add("</body>");
            reports.Add("</html>");
            return Encoding.UTF8.GetBytes(string.Join("", reports));
        }


        public static Dictionary<int, string> ColumnSit
        {
            get
            {
                return new Dictionary<int, string>() {
                    {4, "SitName" },
                    {5, "CodeName"},
                    {6, "SitPrior"},
                    {7, "MsgName"},
                    {8, "CountObj"},
                    {9, "TypeName"},
                    {10, "Comm"}
                };
            }
        }

        public static Dictionary<int, string> ColumnAbon
        {
            get
            {
                return new Dictionary<int, string>() {
                    {4, "AbName" },
                    {5, "DepName"},
                    {6, "Position"},
                    {7, "AbPrior"},
                    {8, "StatusName"},
                    {9, "TypeName"},
                    {10, "LocName"},
                    {11, "ConnParam"},
                    {12, "Address"},
                    {13, "AbComm"},
                    {14, "label_name_value_field_list"}
                };
            }
        }

        public static Dictionary<int, string> ColumnASO
        {
            get
            {
                return new Dictionary<int, string>() {
                    {4, "SitName" },
                    {5, "AbName"},
                    {6, "DepName"},
                    {7, "Position"},
                    {8, "AbPrior"},
                    {9, "ResultName"},
                    {10, "Time"},
                    {11, "ConnParam"},
                    {12, "CountCall"},
                    {13, "MsgName"},
                    {33, "Details"}
                };
            }
        }

        public static Dictionary<int, string> ColumnSZS
        {
            get
            {
                return new Dictionary<int, string>() {
                    {4, "TSitName" },
                    {5, "TObjName"},
                    {6, "TDepart"},
                    {7, "TStatus"},
                    {8, "TTime"},
                    {9, "TConnect"},
                    {10, "TCount"}
                };
            }
        }

        public static Dictionary<int, string> ColumnP16
        {
            get
            {
                return new Dictionary<int, string>() {
                    {4, "SitName" },
                    {5, "ObjName"},
                    {6, "StartTime"},
                    {7, "LastTime"},
                    {8, "SuccessFail"},
                    {9, "Success"},
                    {10, "Fail"}
                };
            }
        }

        public static Dictionary<int, string> ColumnEventLog
        {
            get
            {
                return new Dictionary<int, string>() {
                    {4, "module" },
                    {5, "time"},
                    {6, "type"},
                    {7, "code"},
                    {8, "user"},
                    {9, "details"}
                };
            }
        }

        public static Dictionary<int, string> ColumnHistoryCall
        {
            get
            {
                return new Dictionary<int, string>() {
                    {4, "SessID" },
                    {5, "SitName"},
                    {6, "TimeAccess"},
                    {7, "AbName"},
                    {8, "DepName"},
                    {9, "LineName"},
                    {10, "Param"},
                    {11, "ResultName"},
                    {12, "Answer"}
                };
            }
        }


        public static Dictionary<int, string> ColumnCUDetali
        {
            get
            {
                return new Dictionary<int, string>() {
                    {4, "SitName" },
                    {5, "ObjName"},
                    {6, "ObjType"},
                    {7, "ObjDefine"},
                    {8, "StatusName"},
                    {9, "UnitName"}
                };
            }
        }

        public static Dictionary<int, string> ColumnCU
        {
            get
            {
                return new Dictionary<int, string>() {
                    {4, "BTime" },
                    {5, "ETime"},
                    {6, "SitName"},
                    {7, "ObjName"},
                    {8, "StatusName"},
                    {9, "Succes"},
                    {10, "Fail"}
                };
            }
        }

        public static Dictionary<int, string> ColumnChannel
        {
            get
            {
                return new Dictionary<int, string>() {
                    {4, "ChName"},
                    {5, "ChState"},
                    {6, "ChNotReadyLine"},
                    {7, "ChNotController"},
                    {8, "ChAnswer"},
                    {9, "ChNoAnswer"},
                    {10, "ChAbBusy"},
                    {11, "ChAnswerDtmf"},
                    {12, "ChAnswerTicker"},
                    {13, "ChErrorAts"},
                    {14, "ChAnswerFax"},
                    {15, "ChInterError"},
                    {16, "ChAnswerSetup"},
                    {17, "ChUndefinedAnswer"},
                    {18, "ChInfo"}
                };
            }
        }

        public static Dictionary<int, string> ColumnLog
        {
            get
            {
                return new Dictionary<int, string>() {
                    {4, "tSessBeg"},
                    {5, "tSessEnd"},
                    {6, "tSitName"}
                };
            }
        }

    }
}
