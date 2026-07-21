using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Xunit;
using AutoWashPro.BLL.DTOs;

namespace AutoWashPro.Tests
{
    public class EdgeCaseTests
    {
        private IList<ValidationResult> ValidateModel(object model)
        {
            var validationResults = new List<ValidationResult>();
            var ctx = new ValidationContext(model, null, null);
            Validator.TryValidateObject(model, ctx, validationResults, true);
            return validationResults;
        }

        [Fact]
        public void UpdateUserProfileDTO_NameTooLong_ShouldBeInvalid()
        {
            var dto = new UpdateUserProfileDTO
            {
                FullName = new string('A', 256),
                Email = "test@example.com",
                PhoneNumber = "0901234567"
            };

            var results = ValidateModel(dto);
            Assert.Contains(results, r => r.MemberNames.Contains("FullName"));
        }

        [Fact]
        public void UpdateUserProfileDTO_EmailMissingAt_ShouldBeInvalid()
        {
            var dto = new UpdateUserProfileDTO
            {
                FullName = "Valid Name",
                Email = "testexample.com",
                PhoneNumber = "0901234567"
            };

            var results = ValidateModel(dto);
            Assert.Contains(results, r => r.MemberNames.Contains("Email"));
        }

        [Fact]
        public void UpdateUserProfileDTO_EmptyDOB_ShouldBeValid()
        {
            var dto = new UpdateUserProfileDTO
            {
                FullName = "Valid Name",
                Email = "test@example.com",
                PhoneNumber = "0901234567",
                DateOfBirth = null
            };

            var results = ValidateModel(dto);
            Assert.DoesNotContain(results, r => r.MemberNames.Contains("DateOfBirth"));
            // Overall valid if length < 100 is applied later
            // We just ensure no error on DOB
        }

        [Fact]
        public void CreateOrUpdateServiceDTO_HtmlInDescription_ShouldBeInvalid()
        {
            var dto = new CreateOrUpdateServiceDTO
            {
                ServiceName = "Wash",
                Description = "This is a <script>alert(1)</script> description.",
                Prices = new List<CreateServicePriceDTO>
                {
                    new CreateServicePriceDTO { VehicleTypeId = 1, BranchId = 1, Price = 100, EstimatedDurationMinutes = 30 }
                }
            };

            var results = ValidateModel(dto);
            Assert.Contains(results, r => r.MemberNames.Contains("Description"));
        }

        [Fact]
        public void CreateOrUpdateServiceDTO_MissingImage_ShouldBeValid()
        {
            // Note: Currently DTO doesn't even have an Image field. We simulate "missing image" by creating standard valid DTO
            var dto = new CreateOrUpdateServiceDTO
            {
                ServiceName = "Wash",
                Description = "Normal description",
                Prices = new List<CreateServicePriceDTO>
                {
                    new CreateServicePriceDTO { VehicleTypeId = 1, BranchId = 1, Price = 100, EstimatedDurationMinutes = 30 }
                }
            };

            var results = ValidateModel(dto);
            // It should be valid
            Assert.Empty(results);
        }
    }
}
