﻿using Microsoft.AspNetCore.Mvc;
using Scheduler.Domain.Repositories;
using Scheduler.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Scheduler.Filters;
using Scheduler.Web.Controllers;
using Scheduler.Domain.Specifications;

namespace Scheduler.Controllers;

/// <summary>
/// Renders <see cref="League"/> views.
/// </summary>
[Authorize(Roles = Role.ADMIN)]
public sealed class LeagueController : Controller
{
	/// <summary>
	/// The repository to execute commands and queries against for <see cref="League"/>.
	/// </summary>
	private readonly ILeagueRepository leagueRepository;

	/// <summary>
	/// Initializes the <see cref="LeagueController"/> class.
	/// </summary>
	/// <param name="leagueRepository">The repository to execute commands and queries against for <see cref="League"/>.</param>
	public LeagueController(ILeagueRepository leagueRepository)
	{
		this.leagueRepository = leagueRepository;
	}

	/// <summary>
	/// Displays the <see cref="Add"/> view.
	/// </summary>
	/// <returns>A form for creating a <see cref="League"/>.</returns>
	[TypeFilter(typeof(ChangePasswordFilter))]
	public IActionResult Add()
	{
		return this.View();
	}

	/// <summary>
	/// Handles POST request for adding a <see cref="League"/>.
	/// </summary>
	/// <param name="league"><see cref="League"/> values.</param>
	/// <param name="teamIds">Teams in the league.</param>
	/// <returns></returns>
	[HttpPost]
	[TypeFilter(typeof(ChangePasswordFilter))]
	public async ValueTask<IActionResult> Add(League league, Guid[] teamIds)
	{
		if (!this.ModelState.IsValid)
		{
			return this.View(league);
		}

		await this.leagueRepository.AddAsync(league);

		return this.RedirectToAction(
			nameof(DashboardController.Leagues),
			"Dashboard");
	}

	/// <summary>
	/// Displays the <see cref="Details(Guid)"/> view.
	/// </summary>
	/// <param name="id">The identifier of the <see cref="League"/> to detail.</param>
	/// <returns>A page with <see cref="League"/> details.</returns>
	[TypeFilter(typeof(ChangePasswordFilter))]
	public async Task<IActionResult> Details(Guid id)
	{
		League? league = (await this.leagueRepository
			.SearchAsync(new ByIdSpecification<League>(id)))
			.FirstOrDefault();

		return league is null
			? this.BadRequest()
			: this.View(league);
	}

	/// <summary>
	/// POST request to delete a <see cref="League"/>.
	/// </summary>
	/// <param name="id">The identifier of the <see cref="League"/> to delete.</param>
	/// <returns>Redirected to <see cref="DashboardController.Leagues(ILeagueRepository)"/>.</returns>
	[HttpPost]
	[TypeFilter(typeof(ChangePasswordFilter))]
	public async Task<IActionResult> Remove(Guid id)
	{
		await this.leagueRepository.RemoveAsync(id);

		return this.RedirectToAction(
			nameof(DashboardController.Leagues),
			"Leagues");
	}
}
