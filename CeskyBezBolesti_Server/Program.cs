using CeskyBezBolesti_Server;
using CeskyBezBolesti_Server.Database;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
string connectionString = "YourSQLiteConnectionString";
builder.Services.AddSingleton<IDatabaseManager>(provider => new SqliteDbManager(connectionString));
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection(nameof(AppSettings)));
GeneralFunctions.Initialize(builder.Configuration);

// Adding CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "default", policy =>
    {
        //policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod(); DEV ONLY
        policy.WithOrigins("https://ceskybezbolesti.vercel.app/", "https://ceskybezbolesti.com/", "https://ceskybezbolesti.cz/");
    });
});

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}



app.UseHttpsRedirection();

app.UseCors("default");


app.UseAuthorization();

app.MapControllers();

app.Run();
