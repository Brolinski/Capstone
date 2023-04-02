﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scheduler.Domain.Models;
using Scheduler.Infrastructure.Extensions;
using Scheduler.Infrastructure.Persistence;

namespace Scheduler.Web.Controllers;

/// <summary>
/// Renders Scheduler management views.
/// </summary>
[Authorize]
public sealed class DashboardController : Controller
{
	/// <summary>
	/// The database to query.
	/// </summary>
	private readonly SchedulerContext context;

	/// <summary>
	/// The variable to manage users.
	/// </summary>
	private readonly UserManager<User> userManager;

	/// <summary>
	/// Initializes the <see cref="DashboardController"/> class.
	/// </summary>
	/// <param name="context">The database to query.</param>
	public DashboardController(SchedulerContext context, UserManager<User> userManager)
	{
		this.context = context;
		this.userManager = userManager;
	}

	/// <summary>
	/// Displays the <see cref="Events"/> view.
	/// </summary>
	/// <returns>A view containing scheduled events.</returns>
	public IActionResult Events()
	{
		var userId = userManager.GetUserId(User);
		IEnumerable<Event> events = this.context.Events
			.WithScheduling()
			.AsRecurring();

		return this.View(events);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="type"></param>
	/// <param name="searchTerm"></param>
	/// <returns></returns>
	[HttpPost]
	public IActionResult Events(
		string? type = null,
		string? searchTerm = null)
	{
		IQueryable<Event> events = type switch
		{
			nameof(Practice) => this.context.Practices
				.Include(p => p.Team),

			nameof(Game) => this.context.Games
				.Include(g => g.HomeTeam)
				.Include(g => g.OpposingTeam),

			_ => this.context.Events
		};
		

		if (searchTerm is not null)
		{
			events = events.Where(e => e.Name.Contains(searchTerm));
		}

		return this.View(events
			.WithScheduling()
			.OrderBy(e => e.StartDate)
			.AsRecurring());
	}

	/// <summary>
	/// Displays the <see cref="Teams"/> view.
	/// </summary>
	/// <returns>A table containing all teams.</returns>
	public async Task<IActionResult> Teams()
	{
		IEnumerable<Team> teams = await this.context.Teams
			.Include(t => t.League)
			.Select(t => new Team()
			{
				Id = t.Id,
				Name = t.Name,
				League = new() { Name = t.League!.Name }
			})
			.ToListAsync();

		return this.View(teams);
	}

	/// <summary>
	/// Displays the <see cref="Fields(IFieldService)"/> view.
	/// Only accessible to administrators.
	/// </summary>
	/// <returns>A view containing all fields.</returns>
	[Authorize(Roles = Role.ADMIN)]
	public async Task<IActionResult> Fields()
	{
		IEnumerable<Field> fields = await this.context.Fields.ToListAsync();

		return this.View(fields);
	}

	/// <summary>
	/// Displays the <see cref="Users(UserManager{User})"/> view.
	/// </summary>
	/// <param name="userManager">The service to get users with.</param>
	/// <returns>A table containing all users.</returns>
	[Authorize(Roles = Role.ADMIN)]
	public async Task<IActionResult> Users(
		[FromServices] UserManager<User> userManager)
	{
		IEnumerable<User> fields = await userManager.Users.ToListAsync();

		return this.View(fields);
	}
}
