﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scheduler.Core.Services;
using Scheduler.Core.Models;

namespace Scheduler.Web.Controllers;

/// <summary>
/// Redners views which display <see cref="Game"/> models.
/// </summary>
[Authorize]
public sealed class GameController : Controller
{
	/// <summary>
	/// The service to query <see cref="Game"/> models with.
	/// </summary>
	private readonly IScheduleService scheduleService;

	/// <summary>
	/// Initializes the <see cref="GameController"/> class.
	/// </summary>
	/// <param name="eventService">The service to query <see cref="Game"/> models with.</param>
	public GameController(IScheduleService eventService)
	{
		this.scheduleService = eventService;
	}

	/// <summary>
	/// Creates a <see cref="Game"/> event.
	/// </summary>
	/// <param name="game"><see cref="Game"/> values.</param>
	/// <returns>
	/// Redirected to <see cref="ScheduleController.Index"/> if successful.
	/// Returned to <see cref="ScheduleController.Create(string)"/> otherwise.
	/// </returns>
	[HttpPost]
	public async Task<IActionResult> Create(Game game)
	{
		if (!this.ModelState.IsValid)
		{
			this.ViewData["EventType"] = nameof(Game);

			return this.View("~/Views/Schedule/Create.cshtml", game);
		}

		await this.scheduleService.CreateAsync(game);

		return this.RedirectToAction(nameof(ScheduleController.Index), "Schedule");
	}

	/// <summary>
	/// Updates a <see cref="Game"/>.
	/// </summary>
	/// <param name="game"><see cref="Game"/> values, <see cref="Event.Id"/> referencing the <see cref="Game"/> to update.</param>
	/// <returns>
	/// Redirected to <see cref="ScheduleController.Index"/>.
	/// Returned to <see cref="ScheduleController.Update(Guid, string)"/> otherwise.
	/// </returns>
	[HttpPost]
	public async Task<IActionResult> Update(Game game)
	{
		if (!this.ModelState.IsValid)
			return this.RedirectToAction(
				nameof(ScheduleController.Update),
				"Schedule",
				new { type = nameof(Game) });

		await this.scheduleService.UpdateAsync(game);

		return this.RedirectToAction(nameof(ScheduleController.Index), "Schedule");
	}
}
