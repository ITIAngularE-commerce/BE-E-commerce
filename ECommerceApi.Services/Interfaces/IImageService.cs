using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerceApi.Services.Interfaces
{
    public interface IImageService
    {
        Task<string> UploadAsync(IFormFile file, string folder);
        Task<bool> DeleteAsync(string publicId);
    }
}
