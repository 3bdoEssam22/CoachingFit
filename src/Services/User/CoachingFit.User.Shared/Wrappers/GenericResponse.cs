namespace CoachingFit.User.Shared.Wrappers
{
    public class GenericResponse<T>
    {
        public int StatusCode { get; set; }
        public string Message { get; set; } = null!;
        public T? Data { get; set; }
    }
}
