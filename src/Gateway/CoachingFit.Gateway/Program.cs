namespace CoachingFit.Gateway
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // YARP
            builder.Services.AddReverseProxy()
                .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

            builder.Services.AddHealthChecks();

            var app = builder.Build();

            app.UseHttpsRedirection();
            app.MapHealthChecks("/health");
            app.MapReverseProxy();

            app.Run();
        }
    }
}