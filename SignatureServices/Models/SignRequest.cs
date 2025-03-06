namespace TokenServices.Models
{
    public class SignRequest
    {
        public required string LibraryPath { get; set; }
        public required string Pin { get; set; }
        public required string Data { get; set; }
    }
}