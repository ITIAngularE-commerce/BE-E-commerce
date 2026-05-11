
namespace ECommerceApi.Services.Implementations
{
    public class ImageService(IConfiguration config) : IImageService
    {
        private Cloudinary BuildCloudinary()
        {
            var account = new Account(
                config["Cloudinary:CloudName"],
                config["Cloudinary:ApiKey"],
                config["Cloudinary:ApiSecret"]);
            return new Cloudinary(account) { Api = { Secure = true } };
        }

        public async Task<string> UploadAsync(IFormFile file, string folder)
        {
            await using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder,
                Transformation = new Transformation().Quality("auto").FetchFormat("auto")
            };

            var cloudinary = BuildCloudinary();
            var result = await cloudinary.UploadAsync(uploadParams);
            return result.SecureUrl.ToString();
        }

        public async Task<bool> DeleteAsync(string publicId)
        {
            var cloudinary = BuildCloudinary();
            var deleteParams = new DeletionParams(publicId);
            var result = await cloudinary.DestroyAsync(deleteParams);
            return result.Result == "ok";
        }
    }

}
