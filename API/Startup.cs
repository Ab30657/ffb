using System.Threading.Tasks;
using System.Text;
using API.Data;
using API.Interfaces;
using API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using API.Entities;
using Microsoft.AspNetCore.Identity;
using API.Helpers;
using API.SignalR;

namespace API
{
	public class Startup
	{
		private readonly IConfiguration _config;
		public Startup(IConfiguration config)
		{
			_config = config;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddSingleton<PresenceTracker>();
			services.Configure<CloudinarySettings>(_config.GetSection("CloudinarySettings"));
			services.AddAutoMapper(typeof(AutoMapperProfiles).Assembly);
			services.AddScoped<IUnitOfWork, UnitOfWork>();
			services.AddScoped<LogUserActivity>();
			services.AddScoped<IPhotoService, PhotoService>();
			services.AddScoped<ITokenService, TokenService>();
			services.AddDbContext<DataContext>(options =>
			{
				options.UseSqlite(_config.GetConnectionString("DefaultConnection"));
			});
			services.AddControllers();
			services.AddCors();
			services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(opt =>
			{
				opt.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuerSigningKey = true,
					IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["TokenKey"])),
					ValidateIssuer = false,
					ValidateAudience = false,
				};

				opt.Events = new JwtBearerEvents
				{
					OnMessageReceived = context =>
					{
						var accessToken = context.Request.Query["access_token"];
						var path = context.HttpContext.Request.Path;
						if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
						{
							context.Token = accessToken;
						}
						return Task.CompletedTask;
					}
				};
			});

			services.AddIdentityCore<AppUser>(opt =>
			{

			}).AddRoles<AppRole>().AddRoleManager<RoleManager<AppRole>>().AddSignInManager<SignInManager<AppUser>>().AddRoleValidator<RoleValidator<AppRole>>().AddEntityFrameworkStores<DataContext>();

			services.AddSignalR();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseHttpsRedirection();

			app.UseRouting();

			app.UseCors(x => x.AllowAnyHeader().AllowAnyMethod().AllowCredentials().WithOrigins("https://localhost:4200"));

			app.UseAuthentication();

			app.UseAuthorization();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
				endpoints.MapHub<PresenceHub>("hubs/presence");
				endpoints.MapHub<MessageHub>("hubs/message");
				endpoints.MapHub<MembersHub>("hubs/members");
			});
		}
	}
}
