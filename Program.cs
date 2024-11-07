using System.Globalization;
using System.Text;
using DataBaseCDF;
using DataBaseCDF.Services;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MySqlConnector;

var cultureInfo = new CultureInfo("fr-FR");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

Env.Load();
var key = Environment.GetEnvironmentVariable("KEY_TOKEN");
if (key == null)
    throw new NullReferenceException("Il est nécessaire d'avoir un KEY_TOKEN .env pour lancer le site web.");

DataBase.cdf = new MySqlDataSource($"Server=localhost;Port=3306;Database=CDF;User={Environment.GetEnvironmentVariable("DB_CDF_USER")};" + 
                                   $"Password={Environment.GetEnvironmentVariable("DB_CDF_PASS")};");

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting(options => { options.LowercaseUrls = true; });
builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (context.Request.Cookies.ContainsKey("jwt"))
                context.Token = context.Request.Cookies["jwt"];

            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            context.Response.Redirect("/login");
            context.HandleResponse();
            return Task.CompletedTask;
        },
        OnTokenValidated = async context =>
        {
            if (context.Principal == null)
                context.Fail("Le compte n'a pas pu être récupéré !");

            var user = context.Principal!;

            await using var connexion = await DataBase.cdf.OpenConnectionAsync();
            await using var commands = new MySqlCommand("SELECT version FROM members WHERE id = @id;", connexion);
            commands.Parameters.AddWithValue("id", user.GetId());

            if (await commands.ExecuteScalarAsync() is int version && version == user.GetVersion())
                return;

            context.Fail("Votre compte a été modifié récemment. Pour des raisons de sécurité, vous devez vous reconnecter.");
        }
    };

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
    };
});
builder.Services.AddSingleton(_ => new JwtTokenService(key));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");
app.MapControllerRoute(name: "default", pattern: "{controller=Users}/{action=Index}/{id?}");

app.Run();


