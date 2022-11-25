using System;
using MiniApp;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


string path = $"{Environment.CurrentDirectory}";

if (path.StartsWith("/"))
{
    path = path + "/";
}
else
{
    path = path + "\\";
}
path = path + "config.sqlServer";

string conStr = "";

using (StreamReader sr = new StreamReader(path, true))
{
    conStr = sr.ReadToEnd();
    sr.Close();
}

builder.Services.AddDbContext<SqlServerContext>(
    options => options.UseSqlServer(conStr)
    );



var app = builder.Build();

//app.UseSession();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}

app.UseHttpsRedirection();


app.UseAuthorization();

app.UseStaticFiles();

app.MapControllers();

app.Run();

