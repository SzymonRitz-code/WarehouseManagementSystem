using Microsoft.AspNetCore.Mvc.RazorPages;
using Serilog;

namespace IdentityServer;

internal static class HostingExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddRazorPages();

        var isBuilder = builder.Services.AddIdentityServer(options =>
        {
            options.Events.RaiseErrorEvents = true;
            options.Events.RaiseInformationEvents = true;
            options.Events.RaiseFailureEvents = true;
            options.Events.RaiseSuccessEvents = true;
            // Tokens must contain the browser-visible issuer rather than the Docker service name.
            options.IssuerUri = builder.Configuration["IdentityServer:IssuerUri"];

            // Umożliwia rządania na HTTP w dev 
            //options.EmitStaticAudienceClaim = true; 
        });

        // External User Service
        isBuilder.Services.AddScoped<IFakeUserService, FakeUserService>();

        // in-memory, code config
        isBuilder.AddInMemoryIdentityResources(Config.IdentityResources);
        isBuilder.AddInMemoryApiResources(Config.ApiResources);
        isBuilder.AddInMemoryApiScopes(Config.ApiScopes);
        isBuilder.AddInMemoryClients(Config.Clients);


        //if you want to use server-side sessions: https://blog.duendesoftware.com/posts/20220406_session_management/
        //then enable it
        isBuilder.AddServerSideSessions();

        builder.Services.AddAuthorization();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowWmsClient",
                policy =>
                {
                    policy.WithOrigins("https://localhost:4200", "https://localhost:4201")
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
        });
        //and put some authorization on the admin / management pages
        //builder.Services.AddAuthorization(options =>
        //       options.AddPolicy("admin",
        //           policy => policy.RequireClaim("sub", "1"))
        //   );
        builder.Services.Configure<RazorPagesOptions>(options =>
            options.Conventions.AuthorizeFolder("/ServerSideSessions", "admin"));


        builder.Services.AddAuthentication();
        //.AddGoogle(options =>
        //{
        //  options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;

        //  // register your IdentityServer with Google at https://console.developers.google.com
        //  // enable the Google+ API
        //  // set the redirect URI to https://localhost:5001/signin-google
        //  options.ClientId = "copy client ID from Google here";
        //  options.ClientSecret = "copy client secret from Google here";
        //});

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseCors("AllowWmsClient");
        app.UseIdentityServer();
        app.UseAuthorization();

        app.MapGet("/api/users", (IFakeUserService fakeUserService) =>
            Results.Ok(fakeUserService.GetUserSummaries()));
        app.MapGet("/api/users/{subjectId}", (string subjectId, IFakeUserService fakeUserService) =>
        {
            var user = fakeUserService.GetUserSummary(subjectId);
            return user is null ? Results.NotFound() : Results.Ok(user);
        });

        app.MapRazorPages()
            .RequireAuthorization();

        return app;
    }
}
