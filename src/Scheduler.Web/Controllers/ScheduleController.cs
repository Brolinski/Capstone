﻿using Microsoft.AspNetCore.Mvc;
using Scheduler.Core.Services;
using Scheduler.Core.Models;

namespace Scheduler.Web.Controllers;

/// <summary>
/// Renders views for scheduling events.
/// </summary>
public sealed class ScheduleController : Controller
{
	/// <summary>
	/// The service to query <see cref="Event"/> and it's children with.
	/// </summary>
	private readonly IScheduleService scheduleService;

	/// <summary>
	/// Initializes the <see cref="ScheduleController"/> class.
	/// </summary>
	/// <param name="scheduleService">The service to query <see cref="Event"/> and it's children with.</param>
	public ScheduleController(IScheduleService scheduleService)
	{
		this.scheduleService = scheduleService;
	}

	/// <summary>
	/// Displays the <see cref="Index"/> view.
	/// </summary>
	/// <returns>A table displaying all instances of <see cref="Event"/>.</returns>
	public async Task<IActionResult> Index()
	{
		IEnumerable<Event> events = await this.scheduleService.GetAllAsync();

		return this.View(events);
	}

	/// <summary>
	/// Displays the <see cref="Create(string)"/> view.
	/// </summary>
	/// <param name="type">The type of event to create, defaults to <see cref="Event"/>.</param>
	/// <returns>A form for creating the <see cref="Event"/> or any of it's children.</returns>
	public IActionResult Create(string type = nameof(Event))
	{
		this.ViewData["EventType"] = type;

		return this.View();
	}

	/// <summary>
	/// Displays the <see cref="Update(Guid, string)"/> view.
	/// </summary>
	/// <param name="id">References <see cref="Event.Id"/>.</param>
	/// <param name="type">The type of <see cref="Event"/> to update.</param>
	/// <returns>A form for updating an <see cref="Event"/> or any of it's children.</returns>
	public async Task<IActionResult> Update(Guid id, string type = nameof(Event))
	{
		Event scheduledEvent = await this.scheduleService.GetAsync(id);

		this.ViewData["EventType"] = type;

		return this.View(scheduledEvent);
	}

	/// <summary>
	/// Deletes a <see cref="Event"/>.
	/// </summary>
	/// <param name="id">References <see cref="Event.Id"/>.</param>
	/// <returns>Redirected to <see cref="ScheduleController.Index"/>.</returns>
	[HttpPost]
	public async Task<IActionResult> Delete(Guid id)
	{
		await this.scheduleService.DeleteAsync(id);

		return this.RedirectToAction(nameof(this.Index));
	}
}
