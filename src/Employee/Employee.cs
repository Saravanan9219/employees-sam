using System;
using System.Globalization;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace Models
{
    public class Employee
    {
        [Required]
        public int emp_id { get; set; }
        [Required]
        public string emp_name { get; set; }
        [Required]
        [ValidateEmpType]
        public string emp_type { get; set; }
        [Required]
        [RegularExpression(@"(((0|1)[0-9]|2[0-9]|3[0-1])\-(0[1-9]|1[0-2])\-((19|20)\d\d))$",
         ErrorMessage = "Invalid date format. It should dd-MM-yyyy")]
        public string emp_dob { get; set; }
        [Required]
        [RegularExpression(@"(((0|1)[0-9]|2[0-9]|3[0-1])\-(0[1-9]|1[0-2])\-((19|20)\d\d))$",
         ErrorMessage = "Invalid date format. It should dd-MM-yyyy")]
        public string emp_doj { get; set; }
        [Required]
        [ValidateEmpDepartment]
        public string emp_department { get; set; }

        private class ValidateEmpType : ValidationAttribute
        {
            protected override ValidationResult IsValid(object value, ValidationContext validationContext)
            {
                var validValues = new List<string>() { "Fulltime", "Intern", "Contractor" };
                if (validValues.Contains((string)value))
                {
                    return null;
                }
                else
                {
                    return new ValidationResult("Valid choices are Fulltime, Intern, Contractor", new List<string>() { validationContext.MemberName });
                }
            }
        }

        private class ValidateEmpDepartment : ValidationAttribute
        {
            protected override ValidationResult IsValid(object value, ValidationContext validationContext)
            {
                var validValues = new List<string>() { "Finance", "HR", "IT", "Administration" };
                if (validValues.Contains((string)value))
                {
                    return null;
                }
                else
                {
                    return new ValidationResult("Valid choices are Finance, HR, IT, Administration",
                                                new List<string>() { validationContext.MemberName });
                }
            }
        }

        public static bool Validate<T>(T obj, out ICollection<ValidationResult> results)
        {
            results = new List<ValidationResult>();
            return Validator.TryValidateObject(obj, new ValidationContext(obj), results, true);
        }

        public bool isValid(out ICollection<ValidationResult> validationErrors, out Dictionary<string, dynamic> validatedData)
        {
            validationErrors = null;
            validatedData = new Dictionary<string, dynamic>();
            ICollection<ValidationResult> errors = null;
            if (!Validate(this, out errors))
            {
                validationErrors = errors;
                return false;
            }
            else
            {
                validatedData = new Dictionary<string, dynamic> {
                {"emp_id", (long)this.emp_id},
                {"emp_name", this.emp_name},
                {"emp_type", this.emp_type},
                {
                    "emp_dob",
                    DateTime.ParseExact(
                        this.emp_dob, "dd-MM-yyyy",
                        CultureInfo.InvariantCulture
                    ).ToString("yyyy-MM-dd HH:mm:ss")
                },
                {
                    "emp_doj",
                    DateTime.ParseExact(
                        this.emp_doj, "dd-MM-yyyy",
                        CultureInfo.InvariantCulture
                    ).ToString("yyyy-MM-dd HH:mm:ss")
                },
                {"emp_department", this.emp_department}
            };
                return true;
            }

        }

    }
}