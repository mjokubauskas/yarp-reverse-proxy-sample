using Scalar.AspNetCore;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

var cityList = new List<City>
{
    new City { city = "Istanbul", country = "Turkey", population = 15636000 },
    new City { city = "Moscow", country = "Russia", population = 13010000 },
    new City { city = "London", country = "United Kingdom", population = 8982000 },
    new City { city = "Berlin", country = "Germany", population = 3769000 },
    new City { city = "Madrid", country = "Spain", population = 3333000 },
    new City { city = "Rome", country = "Italy", population = 2873000 },
    new City { city = "Paris", country = "France", population = 2148000 },
    new City { city = "Vienna", country = "Austria", population = 1951000 },
    new City { city = "Warsaw", country = "Poland", population = 1863000 },
    new City { city = "Budapest", country = "Hungary", population = 1756000 },
    new City { city = "Barcelona", country = "Spain", population = 1636000 },
    new City { city = "Munich", country = "Germany", population = 1516000 },
    new City { city = "Milan", country = "Italy", population = 1396000 },
    new City { city = "Prague", country = "Czech Republic", population = 1343000 },
    new City { city = "Sofia", country = "Bulgaria", population = 1307000 },
    new City { city = "Brussels", country = "Belgium", population = 1211000 },
    new City { city = "Amsterdam", country = "Netherlands", population = 907000 },
    new City { city = "Stockholm", country = "Sweden", population = 975000 },
    new City { city = "Copenhagen", country = "Denmark", population = 653000 },
    new City { city = "Dublin", country = "Ireland", population = 588000 }
};

app.MapGet("/Cities", () =>
{
    var Cities = cityList;
    
    return Cities;
})
.WithName("GetCities");

app.Run();

class City 
{
    public string city { get; set; }
    public string country { get; set; }
    public int population { get; set; }
}