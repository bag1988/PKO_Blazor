using SMDataServiceProto.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ServerLibrary.Extensions;
using SharedLibrary;
using static Label.V1.Label;
using Google.Protobuf;
using Label.V1;

namespace ServerLibrary.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[action]")]
    [CheckPermission(new int[] { NameBitsPos.General })]
    public class LabelsController : Controller
    {
        private readonly LabelClient _Label;
        private readonly ILogger<LabelsController> _logger;

        public LabelsController(ILogger<LabelsController> logger, LabelClient label)
        {
            _logger = logger;
            _Label = label;
        }


        /// <summary>
        /// Устанавливаем значение для подтверждения пароля
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> UpdateLabelFieldConfirmSituation(OBJ_KeyInt request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Label.UpdateLabelFieldConfirmSituationAsync(request);
                //BoolValue
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        /// <summary>
        /// Получаем значение для подтверждения паролем
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetLabelFieldConfirmBySituation(OBJ_Key request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Label.GetLabelFieldConfirmBySituationAsync(request);
                //IntID
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSituationLabelField([FromBody] string json)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var request = JsonParser.Default.Parse<SituationLabelField>(json);
                var response = await _Label.UpdateSituationLabelFieldAsync(request);
                //IntID
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetSituationLabelField(SitLabelField request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Label.GetSituationLabelFieldAsync(request);
                //SituationLabelField
                return Ok(JsonFormatter.Default.Format(response));
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }

        }

        [HttpPost]
        public async Task<IActionResult> GetLabelAllFiedlForForm(OBJ_Key request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Label.GetLabelAllFiedlForFormAsync(request);
                //List<LabelNameValueField>
                return Ok(response.List);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }

        }


        [HttpPost]
        public async Task<IActionResult> GetLabelAllFiedlForAbonent(OBJ_Key request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Label.GetLabelAllFiedlForAbonentAsync(request);
                //List<LabelNameValueField>
                return Ok(response.List);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }

        }


        [HttpPost]
        public async Task<IActionResult> UpdateLabelFieldAsoAbonent([FromBody] string json)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var request = JsonParser.Default.Parse<LabelFieldAndOBJKey>(json);
                var response = await _Label.UpdateLabelFieldAsoAbonentAsync(request);
                //BoolValue
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }

        }

        [HttpPost]
        public async Task<IActionResult> GetLabelFieldAsoAbonent(OBJ_Key request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _Label.GetLabelFieldAsoAbonentAsync(request);
                //LabelField
                return Ok(JsonFormatter.Default.Format(response));
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }

        }



    }
}
