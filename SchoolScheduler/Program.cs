using SchoolScheduler.Data;
using SchoolScheduler.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// =======================
// Add Controllers
// =======================
builder.Services.AddControllers();

// =======================
// Database Context
// =======================
builder.Services.AddDbContext<SchoolDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// =======================
// Application Services
// =======================
builder.Services.AddScoped<ScheduleService>();

var app = builder.Build();

// =======================
// HTTP PIPELINE
// =======================

// ⚠️ IMPORTANT: HTTPS redirection
app.UseHttpsRedirection();

// ⚠️ Routing must exist before mapping controllers
app.UseRouting();

// Authorization (safe even if not used)
app.UseAuthorization();

// 🔥 CRITICAL: Map controllers (WITHOUT THIS = 404)
app.MapControllers();

app.Run();