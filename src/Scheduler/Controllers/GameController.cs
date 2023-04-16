﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scheduler.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Scheduler.Infrastructure.Persistence;
using Scheduler.Filters;
using Scheduler.Domain.Repositories;
using Scheduler.ViewModels;
using Scheduler.Domain.Specifications.Events;
using Scheduler.Domain.Specifications;
using Scheduler.Extensions;

namespace Scheduler.Web.Controllers.Scheduling;

/// <summary>
/// Handles POST requests for games.
/// </summary>
[Authorize]
public sealed class GameController : ScheduleController<Game>
{
	/// <summary>
	/// Initializes the <see cref="GameController"/> class.
	/// </summary>
	/// <param name="context">The database to query.</param>
	/// <param name="scheduleRepository">The repository to execute commands and queries against.</param>
	public GameController(IScheduleRepository scheduleRepository)
		: base(scheduleRepository)
	{
	}

	/// <summary>
	/// Edits <see cref="Game"/> details.
	/// </summary>
	/// <param name="values"><see cref="Game"/> values.</param>
	/// <returns></returns>
	[HttpPost]
	[TypeFilter(typeof(ChangePasswordFilter))]
	public override async Task<IActionResult> EditDetails(
		Game values, UpdateType updateType)
	{
		if (this.ModelState.IsValid)
		{
			Specification<Event> updateSpec = updateType.ToSpecification(values);

			await this.scheduleRepository.EditGameDetails(
				values, updateSpec);
		}

		return this.View("~/Views/Schedule/Details.cshtml", values);
	}
}
