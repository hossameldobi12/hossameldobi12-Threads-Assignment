using FileStorageDesign.Exceptions;
using FileStorageDesign.Models;
using FileStorageDesign.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileStorageDesign.Services
{
    public class CustomerService
    {
        private readonly  CustomerRepository _customerRepository;

        public CustomerService(CustomerRepository customerRepository)
        {
            _customerRepository = customerRepository;
        }

        public async Task<Customer> GetCustomerAsync(int customerId, CancellationToken cancellationToken)
        {
            var customer = await _customerRepository.GetCunstomerByIdAsync(customerId, cancellationToken);
            if(customer == null)
            {
                throw new CustomerNotFoundException($"Customer with ID {customerId} not found.");
            }
            return customer;
        }
    }
}
