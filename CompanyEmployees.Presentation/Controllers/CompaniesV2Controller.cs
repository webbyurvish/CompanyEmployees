using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Presentation.Controllers
{
    [Route("api/companies")]    [ApiController]
    public class CompaniesV2Controller : ControllerBase
    {
        private readonly IServiceManager _serviceManager;

        public CompaniesV2Controller(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetCompanies()
        {
            var companies = await _serviceManager.CompanyService.GetAllCompaniesAsync(trackChanges: false);

            var companiesV2 = companies.Select(x => $"{x.Name} V2");
            return Ok(companiesV2);
        }
    }
}
