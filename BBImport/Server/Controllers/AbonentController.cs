using System.Data;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using AsoDataProto.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using ReplaceLibrary;
using ServerLibrary.Extensions;
using SharedLibrary;
using SharedLibrary.Extensions;
using SharedLibrary.GlobalEnums;
using SharedLibrary.Models;
using SMDataServiceProto.V1;
using static AsoDataProto.V1.AsoData;
using static SMDataServiceProto.V1.SMDataService;
using static SMSSGsoProto.V1.SMSSGso;
using Int32Value = Google.Protobuf.WellKnownTypes.Int32Value;
using ServerLibrary;

namespace BBImport.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[action]")]
    [CheckPermission(new int[] { NameBitsPos.General })]
    public class AbonentController : Controller
    {
        private readonly SMDataServiceClient _SMData;
        private readonly AsoDataClient _ASOData;
        private readonly SMSSGsoClient _SMSGso;
        private readonly ILogger<AbonentController> _logger;
        private readonly WriteLog _Log;
        private readonly AuthorizedInfo _userInfo;
        private readonly IStringLocalizer<SqlBaseReplace> SqlRep;
        private readonly IStringLocalizer<GSOReplase> GsoRep;
        readonly string DirectoryTmp = System.IO.Path.Combine("wwwroot", "tmp");

        public AbonentController(ILogger<AbonentController> logger, SMDataServiceClient SMData, SMSSGsoClient SMSGso, AsoDataClient ASOData, WriteLog log, AuthorizedInfo userInfo, IStringLocalizer<SqlBaseReplace> sqlRep, IStringLocalizer<GSOReplase> gsoRep)
        {
            _logger = logger;
            _SMData = SMData;
            _ASOData = ASOData;
            _SMSGso = SMSGso;
            _Log = log;
            _userInfo = userInfo;
            SqlRep = sqlRep;
            GsoRep = gsoRep;
        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async IAsyncEnumerable<string> ImportAbonentListWithParams(ReadFileRequestInfo request, [EnumeratorCancellation] CancellationToken token)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            Int32Value response = new();

            IActionResult? actionResult = null;
            int staffId = _userInfo.GetInfo?.LocalStaff ?? 0;
            try
            {
                request.FileRequest.DataSize = -1;
                actionResult = await ReadTmpFileInfoAbon(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                yield break;
            }
            if (actionResult is OkObjectResult)
            {
                try
                {
                    var okResult = actionResult as OkObjectResult;

                    var result = okResult?.Value as List<ASOAbonent>;

                    if (!request.TimeLimit)
                    {
                        request.StartTime = new TimeOnly(0, 0);
                        request.EndTime = new TimeOnly(23, 59);
                    }

                    if (result != null)
                    {
                        foreach (var items in result.GroupBy(x => x.Name))
                        {
                            if (token.IsCancellationRequested)
                                throw new OperationCanceledException();

                            AbonMsgParamList abList = new();
                            AbonInfo ImportAbon = new();
                            OBJ_ID AbonId = new();
                            AbonId.ObjID = 0;
                            AbonId.StaffID = staffId;
                            AbonId.SubsystemID = SubsystemType.SUBSYST_ASO;

                            ImportAbon.Abon = AbonId;

                            ImportAbon.AbName = items.First().Name;
                            ImportAbon.Password = "";
                            ImportAbon.Position = "";
                            ImportAbon.AbComm = GsoRep["IMPORT_AUTO"];
                            ImportAbon.AbPrior = 1;
                            ImportAbon.AbStatus = 1;
                            ImportAbon.Role = 0;

                            ImportAbon.Dep = new();

                            List<Shedule> shedules = new List<Shedule>();

                            var sheduleDefault = new Shedule()
                            {
                                Abon = AbonId,
                                ASOShedule = new OBJ_ID() { StaffID = staffId, SubsystemID = SubsystemType.SUBSYST_ASO },
                                BaseType = (int)BaseLineType.LINE_TYPE_DIAL_UP,
                                GlobalType = 0,
                                UserType = 0,
                                TimeType = request.TimeLimit ? 1 : 0,
                                Begtime = Duration.FromTimeSpan(request.StartTime.ToTimeSpan()),
                                Endtime = Duration.FromTimeSpan(request.EndTime.ToTimeSpan()),
                                DayType = 0,
                                DayWeek = "1111111",
                                ConnType = (int)BaseLineType.LINE_TYPE_DIAL_UP,
                                Loc = new(AbonId) { ObjID = request.SelectLocation },
                                Beeper = 0
                            };
                            foreach (var s in items)
                            {
                                if (!string.IsNullOrEmpty(s.Phone))
                                {
                                    foreach (var p in s.Phone.Split(",;".ToCharArray()))
                                    {
                                        if (request.PhoneLine)
                                        {
                                            shedules.Add(new Shedule(sheduleDefault)
                                            {
                                                BaseType = (int)BaseLineType.LINE_TYPE_DIAL_UP,
                                                ConnType = (int)BaseLineType.LINE_TYPE_DIAL_UP,
                                                Address = s.Addr ?? "",
                                                ConnParam = p
                                            });
                                        }

                                        if (request.SendSms)
                                        {
                                            shedules.Add(new Shedule(sheduleDefault)
                                            {
                                                BaseType = (int)BaseLineType.LINE_TYPE_GSM_TERMINAL,
                                                ConnType = (int)BaseLineType.LINE_TYPE_GSM_TERMINAL,
                                                Address = s.Addr ?? "",
                                                ConnParam = p
                                            });
                                        }
                                    }
                                }
                            }
                            if (shedules.Any())
                            {
                                var abId = await _ASOData.SetAbInfoAsync(ImportAbon);

                                if (abId != null && abId.ID != 0)
                                {
                                    abList.Array.Add(new AbonMsgParam()
                                    {
                                        AbonID = abId.ID,
                                        StaffID = staffId,
                                        SubsystemID = SubsystemType.SUBSYST_ASO,
                                        ParamName = "ДОЛГ",
                                        ParamValue = items.First().Comment ?? ""
                                    });
                                    abList.Array.Add(new AbonMsgParam()
                                    {
                                        AbonID = abId.ID,
                                        StaffID = staffId,
                                        SubsystemID = SubsystemType.SUBSYST_ASO,
                                        ParamName = "ФИО",
                                        ParamValue = items.First().Name ?? ""
                                    });
                                    if (!string.IsNullOrEmpty(items.First().Addr))
                                        abList.Array.Add(new AbonMsgParam()
                                        {
                                            AbonID = abId.ID,
                                            StaffID = staffId,
                                            SubsystemID = SubsystemType.SUBSYST_ASO,
                                            ParamName = "АДРЕС",
                                            ParamValue = items.First().Addr ?? ""
                                        });
                                    shedules.ForEach(x => x.Abon.ObjID = abId.ID);

                                    SheduleList r = new();
                                    r.Array.AddRange(shedules);
                                    await _ASOData.SetSheduleInfoAsync(r);

                                    yield return JsonFormatter.Default.Format(abList);
                                }
                            }
                        }

                    }
                }
                finally
                {

                }
            }
            yield break;
        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> DeleteAbonentList()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            Int32Value response = new();
            try
            {
                response = await _ASOData.DeleteAbonentListAsync(new Empty());
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(response);
        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> ClearMsgParam()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            Int32Value response = new();
            try
            {
                response = await _ASOData.ClearMsgParamAsync(new Empty());
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(response);
        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> ReadTmpFileImport(ReadFileRequest readFileRequest)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                readFileRequest.FileName = System.IO.Path.Combine(DirectoryTmp, readFileRequest.FileName);

                if (!Directory.Exists(DirectoryTmp))
                {
                    Directory.CreateDirectory(DirectoryTmp);
                }
                if (!System.IO.File.Exists(readFileRequest.FileName))
                {
                    return NoContent();
                }
                switch (readFileRequest.ContentType)
                {
                    case "text/plain":
                        return await ReadTxtFile(readFileRequest);
                    case "text/xml":
                        return ReadXmlFile(readFileRequest);
                    case "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet":
                        return ReadXlsxFile(readFileRequest);
                        //           //case "application/vnd.openxmlformats-officedocument.wordprocessingml.document": MessageView?.AddError("", GsoRep["FORMAT_NO_SUPPORT"]); break;//docx
                        //           //case "application/vnd.ms-excel": MessageView?.AddError("", GsoRep["FORMAT_NO_SUPPORT"]); break;//no support
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
            return NoContent();
        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> ReadTmpFileInfoAbon(ReadFileRequestInfo request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                request.FileRequest.NumberPage = request.NumberPage;
                if (request.FileRequest.FirstStringAsName && request.FileRequest.NumberPage == 0)
                {
                    request.FileRequest.DataSize += 1;
                }
                var actionResult = await ReadTmpFileImport(request.FileRequest);

                if (actionResult is OkObjectResult)
                {
                    var okResult = actionResult as OkObjectResult;

                    var result = okResult?.Value as List<List<string>>;

                    if (result != null)
                    {
                        List<ASOAbonent> ImportAbonList = new();

                        if (request.FileRequest.FirstStringAsName && request.FileRequest.NumberPage == 0)
                        {
                            result.RemoveAt(0);
                        }
                        var f = result.Where(x => (!string.IsNullOrEmpty(x.ElementAtOrDefault(request.ColumnInfo.SurnameColumn)) || (request.ContractNumber && !string.IsNullOrEmpty(x.ElementAtOrDefault(request.ColumnInfo.ContractColumn)))) && !string.IsNullOrEmpty(x.ElementAtOrDefault(request.ColumnInfo.PhoneColumn)));

                        //int indexElem = 1;
                        foreach (var abon in f)
                        {
                            ASOAbonent info = new();
                            //info.Stat = indexElem++;
                            info.Name = abon.ElementAtOrDefault(request.ColumnInfo.SurnameColumn);
                            if (request.ContractNumber && !string.IsNullOrEmpty(abon.ElementAtOrDefault(request.ColumnInfo.ContractColumn)))
                            {
                                info.Name += ($" ({abon.ElementAtOrDefault(request.ColumnInfo.ContractColumn)})");
                            }
                            info.Phone = abon.ElementAtOrDefault(request.ColumnInfo.PhoneColumn);

                            if (!string.IsNullOrEmpty(info.Phone))
                            {
                                var phoneList = info.Phone.Split(",;".ToCharArray());

                                if (phoneList.Length > 0)
                                {
                                    for (var i = 0; i < phoneList.Length; i++)
                                    {
                                        phoneList[i] = Regex.Replace(phoneList[i], @"\D", "", RegexOptions.Compiled);
                                        if (phoneList[i].Length > 0 && request.WaitingTone && phoneList[i][0] == '8')
                                            phoneList[i] = phoneList[i].Insert(1, "w");
                                    }
                                    info.Phone = string.Join(",", phoneList);
                                }
                            }
                            info.Comment = abon.ElementAtOrDefault(request.ColumnInfo.ArrearsColumn);//задолженность

                            if (!string.IsNullOrEmpty(info.Comment) && request.RoundUp)
                            {
                                double resultCost = 0;
                                if (double.TryParse(info.Comment.Replace('.', ','), out resultCost))
                                    info.Comment = ((Int32)Math.Round(resultCost, 2)).ToString();

                                if (request.AccountDebt && resultCost == 0)
                                    continue;
                            }

                            info.Addr = abon.ElementAtOrDefault(request.ColumnInfo.AddressColumn);
                            info.Pos = abon.ElementAtOrDefault(request.ColumnInfo.CodeColumn);//код валюты

                            if (request.CurrencyCode && !string.IsNullOrEmpty(info.Comment))
                            {
                                info.Comment += " ";
                                switch (info.Pos)
                                {
                                    case "974":
                                        info.Comment += GsoRep["BYN"]
                                         ; break;
                                    case "643":
                                        info.Comment += GsoRep["RUB"]
                                        ; break;
                                    case "840":
                                        info.Comment += GsoRep["USD"]
                                         ; break;
                                    case "978":
                                        info.Comment += GsoRep["EUR"]
                                         ; break;
                                    default:
                                        info.Comment += GsoRep["MONEY"]
                                         ; break;
                                }
                            }
                            ImportAbonList.Add(info);
                        }
                        return Ok(ImportAbonList);
                    }

                }
                else
                    return BadRequest();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
            }
            return NoContent();
        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public IActionResult ReadWorkSheets([FromBody] string FileName)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            Dictionary<string, string> WorkSheets = new();
            try
            {
                FileName = System.IO.Path.Combine(DirectoryTmp, FileName);

                if (!Directory.Exists(DirectoryTmp))
                {
                    Directory.CreateDirectory(DirectoryTmp);
                }
                if (System.IO.File.Exists(FileName))
                {

                    using (SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.Open(FileName, false))
                    {
                        WorkbookPart? workbookPart = spreadsheetDocument.WorkbookPart;
                        if (workbookPart != null)
                        {
                            var sheets = workbookPart.Workbook.Descendants<Sheet>();

                            foreach (var sheet in sheets)
                            {
                                if (sheet.Id?.Value != null && sheet.Name?.Value != null)
                                {
                                    WorkSheets.Add(sheet.Id.Value, sheet.Name.Value);
                                }
                            }
                        }

                    }
                }
                return Ok(WorkSheets);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        async Task<IActionResult> ReadTxtFile(ReadFileRequest readFileRequest)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                System.Text.Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                List<List<string>> abonInfo = new();
                int startIndex = readFileRequest.NumberPage * readFileRequest.DataSize;
                using (var reader = new StreamReader(readFileRequest.FileName, encoding: readFileRequest.CodePage > 0 ? System.Text.Encoding.GetEncoding(readFileRequest.CodePage) : System.Text.Encoding.Default))
                {
                    string? line = null;

                    int IgnoreCount = 0;

                    while (!reader.EndOfStream)
                    {
                        line = await reader.ReadLineAsync(HttpContext.RequestAborted);

                        if (IgnoreCount < readFileRequest.IgnoreStrFirstCount)
                        {
                            IgnoreCount++;
                            continue;
                        }

                        if (startIndex > 0)
                        {
                            startIndex--;
                            continue;
                        }

                        if (abonInfo.Count >= readFileRequest.DataSize && readFileRequest.DataSize > 0)
                            break;

                        if (line == null)
                            continue;

                        abonInfo.Add(line.Split(readFileRequest.Separotor.ToCharArray()).Select(x => x.Trim()).ToList());
                    }
                }

                return Ok(abonInfo);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(ReadTxtFile));
                return BadRequest();
            }
        }

        IActionResult ReadXmlFile(ReadFileRequest readFileRequest)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                XmlDocument doc = new();
                doc.Load(readFileRequest.FileName);
                List<List<string>> abonInfo = new();
                int startIndex = readFileRequest.NumberPage * readFileRequest.DataSize;
                if (doc.DocumentElement != null && doc.DocumentElement.HasChildNodes)
                {
                    int IgnoreCount = 0;
                    foreach (XmlNode node in doc.DocumentElement.ChildNodes)
                    {
                        if (HttpContext.RequestAborted.IsCancellationRequested)
                            throw new OperationCanceledException();

                        if (IgnoreCount < readFileRequest.IgnoreStrFirstCount)
                        {
                            IgnoreCount++;
                            continue;
                        }

                        if (startIndex > 0)
                        {
                            startIndex--;
                            continue;
                        }

                        if (abonInfo.Count >= readFileRequest.DataSize && readFileRequest.DataSize > 0)
                            break;

                        if (node.HasChildNodes)
                        {
                            List<string> childList = new();
                            foreach (XmlNode childNode in node.ChildNodes)
                            {
                                childList.Add(childNode.InnerText.Trim());
                            }
                            abonInfo.Add(childList);
                        }
                    }
                }

                return Ok(abonInfo);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(ReadXmlFile));
                return BadRequest();
            }
        }

        IActionResult ReadXlsxFile(ReadFileRequest readFileRequest)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                List<List<string>> abonInfo = new();
                int startIndex = readFileRequest.NumberPage * readFileRequest.DataSize;

                using (SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.Open(readFileRequest.FileName, false))
                {
                    WorkbookPart? workbookPart = spreadsheetDocument.WorkbookPart;

                    if (workbookPart == null)
                        return NoContent();

                    if (string.IsNullOrEmpty(readFileRequest.SelectSheet))
                    {
                        readFileRequest.SelectSheet = workbookPart.Workbook.Descendants<Sheet>().First().Id?.Value ?? "";
                    }

                    if (string.IsNullOrEmpty(readFileRequest.SelectSheet))
                        return NoContent();

                    using OpenXmlReader reader = OpenXmlReader.Create(workbookPart.GetPartById(readFileRequest.SelectSheet));
                    int columnIndex = 0;
                    int IgnoreCount = 0;
                    while (reader.Read())
                    {
                        if (HttpContext.RequestAborted.IsCancellationRequested)
                            throw new OperationCanceledException();

                        if (reader.ElementType == typeof(Row))
                        {
                            if (IgnoreCount < readFileRequest.IgnoreStrFirstCount)
                            {
                                IgnoreCount++;
                                continue;
                            }

                            if (startIndex > 0)
                            {
                                startIndex--;
                                continue;
                            }

                            if (abonInfo.Count >= readFileRequest.DataSize && readFileRequest.DataSize > 0)
                                break;

                            reader.ReadFirstChild();
                            List<string> childList = new();
                            do
                            {
                                if (reader.ElementType == typeof(Cell))
                                {
                                    Cell? c = (Cell?)reader.LoadCurrentElement();

                                    string cellValue;

                                    if (c != null && c.DataType != null && c.CellValue != null && c.DataType == CellValues.SharedString && workbookPart.SharedStringTablePart != null)
                                    {
                                        SharedStringItem ssi = workbookPart.SharedStringTablePart.SharedStringTable.Elements<SharedStringItem>().ElementAt(int.Parse(c.CellValue.InnerText));

                                        cellValue = ssi.Text?.Text ?? "";
                                    }
                                    else
                                    {
                                        cellValue = c?.CellValue?.InnerText ?? "";
                                    }

                                    int cellColumnIndex = GetColumnIndex((c?.CellReference?.Value ?? "")) ?? 1;

                                    if (childList.Count < cellColumnIndex - 1)
                                    {
                                        childList.AddRange(Enumerable.Repeat(string.Empty, ((cellColumnIndex - 1) - childList.Count)));
                                    }
                                    childList.Add(cellValue.Trim());
                                }

                                columnIndex++;

                            } while (reader.ReadNextSibling());

                            abonInfo.Add(childList);
                        }
                    }
                }

                return Ok(abonInfo);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(ReadXlsxFile));
                return BadRequest();
            }
        }

        string GetColumnName(int colIndex)
        {
            int div = colIndex;
            string colLetter = string.Empty;
            int mod = 0;

            while (div > 0)
            {
                mod = (div - 1) % 26;
                colLetter = (char)(65 + mod) + colLetter;
                div = (int)((div - mod) / 26);
            }
            return colLetter;
        }

        int? GetColumnIndex(string cellReference)
        {
            if (string.IsNullOrEmpty(cellReference))
            {
                return null;
            }
            string columnReference = Regex.Replace(cellReference.ToUpper(), @"[\d]", string.Empty);

            int columnNumber = -1;
            int mulitplier = 1;

            foreach (char c in columnReference.ToCharArray().Reverse())
            {
                columnNumber += mulitplier * ((int)c - 64);

                mulitplier = mulitplier * 26;
            }
            return columnNumber + 1;
        }

    }
}
