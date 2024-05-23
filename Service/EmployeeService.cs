using AutoMapper;
using Contracts;
using Entities.Exceptions;
using Entities.LinkModels;
using Entities.Models;
using Service.Contracts;
using Shared.DataTransferObjects;
using Shared.RequestFeatures;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IRepositoryManager _repository;
        private readonly ILoggerManager _logger;
        private readonly IMapper _mapper;
        private readonly IEmployeeLinks _employeeLinks;

        public EmployeeService(IRepositoryManager repository, ILoggerManager logger, IMapper mapper, IEmployeeLinks employeeLinks)
        {
            _repository = repository;
            _logger = logger;
            _mapper = mapper;
            _employeeLinks = employeeLinks;
        }

        public async Task<(LinkResponse linkResponse, MetaData metaData)> GetEmployeesAsync(Guid companyId, LinkParameters linkParameters, bool trackChanges)
        {
            if (!linkParameters.employeeParameters.ValidAgeRange)
                throw new MaxAgeRangeBadRequestException();

            await CheckIfCompanyExists(companyId, trackChanges);

            var employeesWithMetaData = await _repository.Employee.GetEmployeesAsync(companyId, linkParameters.employeeParameters, trackChanges);

            var employeesDto = _mapper.Map<IEnumerable<EmployeeDto>>(employeesWithMetaData);

            var links = _employeeLinks.TryGenerateLinks(employeesDto, linkParameters.employeeParameters.Fields, companyId, linkParameters.context);

            return (linkResponse: links, metaData: employeesWithMetaData.MetaData);
        }

        public async Task<EmployeeDto> GetEmployeeAsync(Guid companyId, Guid id, bool trackChanges)
        {
            await CheckIfCompanyExists(companyId, trackChanges);

            var employeeDb = GetEmployeeForCompanyAndCheckIfItExists(companyId, id, trackChanges);

            var employee = _mapper.Map<EmployeeDto>(employeeDb);

            return employee;
        }

        public async Task<EmployeeDto> CreateEmployeeForCompanyAsync(Guid companyId, EmployeeForCreationDto employeeForCreation, bool trackChanges)
        {
            await CheckIfCompanyExists(companyId, trackChanges);

            var employeeEntity = _mapper.Map<Employee>(employeeForCreation);

            _repository.Employee.CreateEmployeeForCompany(companyId, employeeEntity);
            await _repository.SaveAsync();

            var employeeToReturn = _mapper.Map<EmployeeDto>(employeeEntity);

            return employeeToReturn;
        }

        public async Task DeleteEmployeeForCompanyAsync(Guid companyId, Guid id, bool trackChanges)
        {
            await CheckIfCompanyExists(companyId, trackChanges);

            var employeeDb = await GetEmployeeForCompanyAndCheckIfItExists(companyId, id, trackChanges);

            _repository.Employee.DeleteEmployee(employeeDb);
            await _repository.SaveAsync();
        }

        public async Task UpdateEmployeeForCompanyAsync(Guid companyId, Guid id, EmployeeForUpdateDto employeeForUpdate, bool compTrackChanges, bool empTrackChanges)
        {
            await CheckIfCompanyExists(companyId, compTrackChanges);

            var employeeDb = await GetEmployeeForCompanyAndCheckIfItExists(companyId, id, empTrackChanges);

            _mapper.Map(employeeForUpdate, employeeDb);
            await _repository.SaveAsync();
        }

        public async Task<(EmployeeForUpdateDto employeeToPatch, Employee employeeEntity)> GetEmployeeForPatchAsync(Guid companyId, Guid id, bool compTrackChanges, bool empTrackChanges)
        {
            await CheckIfCompanyExists(companyId, compTrackChanges);

            var employeeDb = await GetEmployeeForCompanyAndCheckIfItExists(companyId, id, empTrackChanges);

            var employeeToPatch = _mapper.Map<EmployeeForUpdateDto>(employeeDb);
            return (employeeToPatch, employeeDb);
        }

        public async Task SaveChangesForPatchAsync(EmployeeForUpdateDto employeeToPatch, Employee employeeEntity)
        {
            _mapper.Map(employeeToPatch, employeeEntity);
            await _repository.SaveAsync();
        }

        private async Task CheckIfCompanyExists(Guid id, bool trackChanges)
        {
            var company = await _repository.Company.GetCompanyAsync(id, trackChanges);
            if (company is null)
                throw new CompanyNotFoundException(id);
        }

        private async Task<Employee> GetEmployeeForCompanyAndCheckIfItExists(Guid companyId, Guid id, bool trackChanges)
        {
            var employee = await _repository.Employee.GetEmployeeAsync(companyId, id, trackChanges);
            if (employee is null)
                throw new EmployeeNotFoundException(companyId);

            return employee;
        }
    }
}
