using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerceApi.Services.DTOs.Common
{
    public class ApiResponse<T>
    {
        public bool IsSuccess { get; set; }
        public T? Data { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string>? Errors { get; set; }
        public string? Message { get; set; }  

        public static ApiResponse<T> Success(T data) => new()
        {
            IsSuccess = true,
            Data = data
        };

        public static ApiResponse<T> Success(T data, string message = "Data retrieved successfully.") => new()
        {
            IsSuccess = true,
            Data = data,
            Message = message
        };

        public static ApiResponse<T> Failure(string error) => new()
        {
            IsSuccess = false,
            ErrorMessage = error
        };

        public static ApiResponse<T> Failure(List<string> errors) => new()
        {
            IsSuccess = false,
            Errors = errors
        };

        public static ApiResponse<T> Failure(string message, List<string> errors) => new()
        {
            IsSuccess = false,
            Message = message,
            Errors = errors
        };
    }
}