﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scheduler.Domain.Models;
using Scheduler.Domain.Repositories;
using Scheduler.Domain.Specifications;
using Scheduler.Infrastructure.Extensions;
using Scheduler.Infrastructure.Persistence;
using Scheduler.Filters;
using Microsoft.IdentityModel.Tokens;

namespace Scheduler.Web.Controllers;

/// <summary>
/// Renders scheduler management views.
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
	/// Displays the <see cref="Events(SchedulerContext, string?, string?)"/> view.
	/// Can also be POSTed to in order to provide filtering.
	/// </summary>
	/// <returns>A view containing scheduled events.</returns>
	[TypeFilter(typeof(ChangePasswordFilter))]
	public IActionResult Events()
	{
		return this.View();
	}

	/// <summary>
	/// Builds a list of Games or Practices for the appropriate Coach Event modal.
	/// </summary>
	/// <param name="type">The currently selected type of Event.</param>
	/// <returns>The appropriate ViewComponent.</returns>
	public async Task <IActionResult> coachEvents(string type)
	{
		Guid userId = Guid.Parse(this.userManager.GetUserId(this.User)
			?? throw new NullReferenceException("Could not get current user."));

		IEnumerable<Team>? coachTeams = await this.context.Teams
			.Where(t => t.UserId == userId)
			.ToListAsync();

		IEnumerable<Event>? games = null;
		IEnumerable<Event>? practices = null;
		
		foreach(Team team in coachTeams)
		{
			if (type == nameof(Game))
			{
				IEnumerable<Event> coachGames = await this.context.Games
					.Where(g => g.HomeTeamId == team.Id || g.OpposingTeamId == team.Id)
					.WithScheduling()
					.ToListAsync();

				if (games is null && !coachGames.IsNullOrEmpty())
				{
					games = coachGames;
				}
				else if (!coachGames.IsNullOrEmpty())
				{
					games = games.Concat(coachGames);					
				}				
			}
			else
			{
				IEnumerable<Event> coachPractices = await this.context.Practices
					.Where(g => g.TeamId == team.Id)
					.WithScheduling()
					.ToListAsync();

				if (practices is null && !coachPractices.IsNullOrEmpty())
				{
					practices = coachPractices;
				}
				else if (!coachPractices.IsNullOrEmpty())
				{
					practices = practices.Concat(coachPractices);
				}
			}			
		}

		if (!games.IsNullOrEmpty())
		{
			this.ViewData["Games"] = games.Distinct().ToList();

			this.ViewData["TypeFilterMessage"] = $"Showing all {type}s";
		}

		if (!practices.IsNullOrEmpty())
		{
			this.ViewData["Practices"] = practices.Distinct().ToList();

			this.ViewData["TypeFilterMessage"] = $"Showing all {type}s";
		}

		if (games.IsNullOrEmpty() && practices.IsNullOrEmpty())
		{
			this.ViewData["TypeFilterMessage"] = $"No {type}s found";
		}

		this.ViewData["CoachTeams"] = coachTeams.ToList();

		this.ViewData["Teams"] = this.context.Teams.ToList();

		if (type == nameof(Game))
		{
			return this.ViewComponent("GamesModal");
		}
		else
		{
			return this.ViewComponent("PracticesModal");
		}
	}

	/// <summary>
	/// Filters Games or Practices in the CoachModalTable.
	/// </summary>
	/// <param name="type">The currently selected type of Event.</param>
	/// <param name="start">The currently selected start date to filter.</param>
	/// <param name="end">The currently selected end date to filter.</param>
	/// <param name="searchTerm">The currently selected search term - defaults to null.</param>
	/// <param name="teamName">The currently selected team name - defaults to null.</param>
	/// <returns>The CoachModalTable partial view.</returns>
	public IActionResult filterCoachEvents(string type, DateTime start, DateTime end, string? searchTerm = null, string? teamName = null)
	{
		var userId = userManager.GetUserId(User);

		IQueryable<Team>? coachTeams = this.context.Teams.Where(t => t.UserId.ToString() == userId);

		IQueryable<Event>? games = null;

		IQueryable<Event>? practices = null;

		foreach (Team team in coachTeams)
		{
			if (type == "Game")
			{
				IQueryable<Event> coachGames = this.context.Games
					.Where(g => g.HomeTeamId == team.Id || g.OpposingTeamId == team.Id)
					.WithScheduling();

				if (games.IsNullOrEmpty() && !coachGames.IsNullOrEmpty())
				{
					games = coachGames;
				}
				else if (!coachGames.IsNullOrEmpty())
				{
					games = games.Concat(coachGames);
				}

				if (!games.IsNullOrEmpty())
				{
					games = this.dateSearch(start, end, games);
				}				

				if (!searchTerm.IsNullOrEmpty())
				{
					games = this.nameSearch(searchTerm, type, games);
				}

				if (!teamName.IsNullOrEmpty() && !games.IsNullOrEmpty())
				{
					games = games.AsQueryable().OfType<Game>().Where(game => game.HomeTeam.Name == teamName || game.OpposingTeam.Name == teamName);

					this.ViewData["TeamFilterMessage"] = $"for Team {teamName}";
				}
			}
			else
			{
				IQueryable<Event> coachPractices = this.context.Practices
					.Where(g => g.TeamId == team.Id)
					.WithScheduling();

				if (practices.IsNullOrEmpty() && !coachPractices.IsNullOrEmpty())
				{
					practices = coachPractices;
				}
				else if (!coachPractices.IsNullOrEmpty())
				{
					practices = practices.Concat(coachPractices);
				}

				if (!practices.IsNullOrEmpty())
				{
					practices = this.dateSearch(start, end, practices);
				}

				if (!searchTerm.IsNullOrEmpty())
				{
					practices = this.nameSearch(searchTerm, type, practices);
				}

				if (!teamName.IsNullOrEmpty() && !practices.IsNullOrEmpty())
				{
					practices = practices.AsQueryable().OfType<Practice>().Where(Practice => Practice.Team.Name == teamName);

					this.ViewData["TeamFilterMessage"] = $"for Team {teamName}";
				}
			}
		}

		IEnumerable<Event> filteredGames = null;
		IEnumerable<Event> filteredPractices = null;

		if (!games.IsNullOrEmpty())
		{
			this.ViewData["TypeFilterMessage"] = $"Showing all {type}s";

			filteredGames = games.Distinct().ToList();
		}

		if (!practices.IsNullOrEmpty())
		{
			this.ViewData["TypeFilterMessage"] = $"Showing all {type}s";

			filteredPractices = practices.Distinct().ToList();
		}

		if (games.IsNullOrEmpty() && practices.IsNullOrEmpty())
		{
			this.ViewData["TypeFilterMessage"] = $"No {type}s found";
		}

		this.ViewData["CoachTeams"] = coachTeams.ToList();

		this.ViewData["Teams"] = this.context.Teams.ToList();

		this.ViewData["EventType"] = type;

		if (type == "Game")
		{
			return PartialView("_CoachModalTable", filteredGames);
		}
		else
		{
			return PartialView("_CoachModalTable", filteredPractices);
		}
	}

	/// <summary>
	/// Displays the <see cref="Fields(IFieldService)"/> view.
	/// Only accessible to administrators.
	/// </summary>
	/// <returns>A view containing all fields.</returns>
	[Authorize(Roles = Role.ADMIN)]
	[TypeFilter(typeof(ChangePasswordFilter))]
	public async Task<IActionResult> Fields(
		[FromServices] IFieldRepository fieldRepository)
	{
		GetAllSpecification<Field> searchSpec = new();
		IEnumerable<Field> fields = await fieldRepository.SearchAsync(searchSpec);

		return this.View(fields);
	}

	/// <summary>
	/// Displays the <see cref="Users(UserManager{User})"/> view.
	/// </summary>
	/// <param name="userManager">The service to get users with.</param>
	/// <returns>A table containing all users.</returns>
	[Authorize(Roles = Role.ADMIN)]
	[TypeFilter(typeof(ChangePasswordFilter))]
	public async Task<IActionResult> Users(
		[FromServices] UserManager<User> userManager)
	{
		IEnumerable<User> users = await userManager.Users.ToListAsync();

		return this.View(users);
	}

	/// <summary>
	/// Displays the <see cref="Leagues(ILeagueRepository)"/> view.
	/// </summary>
	/// <param name="leagueRepository">Queries all leagues.</param>
	/// <returns>A view displaying all leagues with pagination.</returns>
	[Authorize]
	[TypeFilter(typeof(ChangePasswordFilter))]
	public async Task<IActionResult> Leagues(
		[FromServices] ILeagueRepository leagueRepository)
	{
		IEnumerable<League> leagues = await leagueRepository.SearchAsync(
			new GetAllSpecification<League>());

		return this.View(leagues);
	}

	/// <summary>
	/// Refreshes the Calendar View Component.
	/// </summary>
	/// <param name="year">The currently selected year.</param>
	/// <param name="month">The currently selecte month.</param>
	/// <returns>The Calendar ViewComponent.</returns>
	public IActionResult refreshCalendar(int? year, int? month)
	{
		this.ViewData["Year"] = year;
		this.ViewData["Month"] = month;

		return this.ViewComponent("Calendar");
	}

	/// <summary>
	/// Functionality for searching the database for Events.
	/// </summary>
	/// <param name="start">The currently selected start date.</param>
	/// <param name="end">The currently selected end date.</param>
	/// <param name="type">The currently selected type of Event.</param>
	/// <param name="searchTerm">The inputted search term - defaults to null.</param>
	/// <param name="teamName">The inputted team name - defaults to null.</param>
	/// <returns>A list of Events.</returns>
	[AllowAnonymous]
	public async Task<IActionResult> searchModal(DateTime start, DateTime end, string type, string? searchTerm = null, string? teamName = null)
	{
		IQueryable<Event> events = type switch
		{
			nameof(Practice) => this.context.Practices
				.Include(p => p.Team)
				.WithScheduling(),

			nameof(Game) => this.context.Games
				.Include(g => g.HomeTeam)
				.Include(g => g.OpposingTeam)
				.WithScheduling(),

			_ => this.context.Events
				.WithScheduling()
		};
		
		events = this.dateSearch(start, end, events);

		if(!searchTerm.IsNullOrEmpty())
		{
			events = this.nameSearch(searchTerm, type, events);
		}

		if(!teamName.IsNullOrEmpty())
		{
			events = this.teamSearch(teamName, type, events);
		}

		if(events.IsNullOrEmpty())
		{
			this.ViewData["Events"] = null;

			this.ViewData["TypeFilterMessage"] = $"No {type}s found";
		}
		else
		{
			this.ViewData["Events"] = events.ToList();

			this.ViewData["TypeFilterMessage"] = $"Showing all {type}s";
		}

		this.ViewData["Teams"] = await this.context.Teams.ToListAsync();
		this.ViewData["Start"] = start;
		this.ViewData["End"] = end;
		if (end > start.AddYears(1))
		{
			this.ViewData["Title"] = $"All {type}s";
		}
		else
		{
			this.ViewData["Title"] = $"All {type}s from {start.ToString("M/dd/y")} to {end.ToString("M/dd/y")}";
		}

		return this.ViewComponent("SearchListModal");
	}

	/// <summary>
	/// Builds data for the Monthly List Modal.
	/// </summary>
	/// <param name="year">The currently selected year.</param>
	/// <param name="month">The currently selected month.</param>
	/// <returns>The List Modal ViewComponent</returns>
	[AllowAnonymous]
	public async Task<IActionResult> monthModal(int year, int month)
	{
		DateTime monthDate = new DateTime(year, month, 1);
		DateTime monthEndDate = monthDate.AddMonths(1);
		this.ViewData["Events"] = await this.dateSearch(monthDate, monthEndDate).ToListAsync();
		this.ViewData["Teams"] = await this.context.Teams.ToListAsync();
		this.ViewData["Start"] = monthDate;
		this.ViewData["End"] = monthEndDate;
		this.ViewData["Title"] = $"Events in {monthDate.ToString("MMMM")}";
		this.ViewData["TypeFilterMessage"] = "Showing all Events";
		return this.ViewComponent("ListModal");
	}

	/// <summary>
	/// Builds data for the Weekly List Modal.
	/// </summary>
	/// <param name="year">The currently selected year.</param>
	/// <param name="month">The currently selected month.</param>
	/// <param name="weekStart">The start of the currently selected week.</param>
	/// <returns>The List Modal ViewComponent.</returns>
	[AllowAnonymous]
	public async Task<IActionResult> weekModal(int year, int month, int weekStart)
	{
		DateTime weekStartDate = new DateTime(year, month, weekStart);
		DateTime weekEndDate = weekStartDate.AddDays(7);
		this.ViewData["Events"] = await this.dateSearch(weekStartDate, weekEndDate).ToListAsync();
		this.ViewData["Teams"] = await this.context.Teams.ToListAsync();
		this.ViewData["Start"] = weekStartDate;
		this.ViewData["End"] = weekEndDate;
		this.ViewData["Title"] = $"Events for the week of {weekStartDate.ToString("M")}";
		this.ViewData["TypeFilterMessage"] = "Showing all Events";
		return this.ViewComponent("ListModal");
	}

	/// <summary>
	/// Builds data for the Day List Modal.
	/// </summary>
	/// <param name="year">The currently selected year.</param>
	/// <param name="month">The currently selected month.</param>
	/// <param name="date">The currently selected date.</param>
	/// <returns>The List Modal ViewComponent.</returns>
	[AllowAnonymous]
	public async Task<IActionResult> dayModal(int year, int month, int date)
	{
		DateTime eventDate = new DateTime(year, month, date);
		this.ViewData["Events"] = await this.dateSearch(eventDate, eventDate).ToListAsync();
		this.ViewData["Teams"] = await this.context.Teams.ToListAsync();
		this.ViewData["Start"] = eventDate; //12:00 AM on the selected day.
		this.ViewData["End"] = eventDate.Date.AddDays(1).AddSeconds(-1); //11:59 PM on the selected day.
		this.ViewData["Title"] = $"Events on {eventDate.ToString("M")}";
		this.ViewData["TypeFilterMessage"] = "Showing all Events";
		return this.ViewComponent("ListModal");
	}

	/// <summary>
	/// Builds data for the Grid Modal.
	/// </summary>
	/// <param name="year">The currently selected year.</param>
	/// <param name="month">The currently selected month.</param>
	/// <param name="date">The currently selected date.</param>
	/// <returns>The Grid Modal ViewComponent.</returns>
	[AllowAnonymous]
	public async Task<IActionResult> gridModal(int year, int month, int date)
	{
		DateTime eventDate = new DateTime(year, month, date);
		this.ViewData["Events"] = await this.dateSearch(eventDate, eventDate).ToListAsync();
		this.ViewData["Fields"] = await this.context.Fields.OrderBy(e => e.Name).ToListAsync();
		this.ViewData["Title"] = $"Scheduling Grid for {eventDate.ToString("M")}";
		this.ViewData["CurrentDate"] = eventDate;
		return this.ViewComponent("GridModal");
	}

	/// <summary>
	/// Functionality for filtering the List Modal.
	/// </summary>
	/// <param name="type">The currently selected type of Event.</param>
	/// <param name="start">The currently selected start date.</param>
	/// <param name="end">The currently selected end date.</param>
	/// <param name="searchTerm">The inputted search term - defaults to null.</param>
	/// <param name="teamName">The inputted team name - defaults to null.</param>
	/// <returns>The List Modal partial view.</returns>
	[AllowAnonymous]
	public async Task<IActionResult> filterModalEvents(string type, DateTime start, DateTime end, string? searchTerm = null, string? teamName = null)
	{
		IQueryable<Event> events = type switch
		{
			nameof(Practice) => this.context.Practices
				.Include(p => p.Team)
				.WithScheduling(),

			nameof(Game) => this.context.Games
				.Include(g => g.HomeTeam)
				.Include(g => g.OpposingTeam)
				.WithScheduling(),

			_ => this.context.Events
				.WithScheduling()
		};

		events = this.dateSearch(start, end, events);

		if (!searchTerm.IsNullOrEmpty())
		{
			events = this.nameSearch(searchTerm, type, events);
		}

		if (!teamName.IsNullOrEmpty())
		{
			events = this.teamSearch(teamName, type, events);
		}

		if (events.IsNullOrEmpty())
		{
			this.ViewData["TypeFilterMessage"] = $"No {type}s found";
		}
		else
		{
			this.ViewData["TypeFilterMessage"] = $"Showing all {type}s";
		}

		this.ViewData["Teams"] = await this.context.Teams.ToListAsync();
		return PartialView("_ListModalTable", events);
	}

	/// <summary>
	/// Builds a list of Events using date parameters.
	/// </summary>
	/// <param name="start">The currently selected start date.</param>
	/// <param name="end">The currently selected end date.</param>
	/// <param name="events">A list of Events - defaults to null.</param>
	/// <returns>A filtered list of Events.</returns>
	public IQueryable<Event> dateSearch(DateTime start, DateTime end, IQueryable<Event>? events = null)
	{
		if (events.IsNullOrEmpty())
		{
			events = this.context.Events
					.WithScheduling();
		}

		return events
			.Where(e => e.StartDate.Date <= end.Date && e.EndDate.Date >= start.Date)
			.OrderBy(e => e.StartDate);
	}

	/// <summary>
	/// Builds a list of Events using a Team name.
	/// </summary>
	/// <param name="teamName">The inputted Team name.</param>
	/// <param name="type">The currently selected type of Event.</param>
	/// <param name="events">A list of Events - defaults to null.</param>
	/// <returns>A filtered list of Events.</returns>
	public IQueryable<Event> teamSearch(string teamName, string type, IQueryable<Event>? events = null)
	{
		if (events.IsNullOrEmpty())
		{
			events = this.context.Events
					.WithScheduling();
		}

		IQueryable<Team> teamList = this.context.Teams;

		Team selectedTeam = teamList.FirstOrDefault(t => t.Name.ToLower() == teamName.ToLower());

		if (selectedTeam == null)
		{
			ViewData["TeamFilterMessage"] = "Team " + teamName + " does not exist";
			return null;			
		}

		IEnumerable<Event>? matchingGames = null;
		IEnumerable<Event>? matchingPractices = null;

		if (type == "Event" || type == "Game")
		{
			matchingGames = events.AsQueryable().OfType<Game>().Where(game => game.HomeTeam.Id == selectedTeam.Id || game.OpposingTeam.Id == selectedTeam.Id);
		}

		if (type == "Event" || type == "Practice")
		{
			matchingPractices = events.AsQueryable().OfType<Practice>().Where(practice => practice.Team.Id == selectedTeam.Id);
		}

		if(matchingGames.IsNullOrEmpty() && matchingPractices.IsNullOrEmpty())
		{
			ViewData["TeamFilterMessage"] = "There are no scheduled " + type + "s for Team " + selectedTeam.Name + "\nduring the selected dates";
			return null;
		}
		else if (matchingGames.IsNullOrEmpty() && !matchingPractices.IsNullOrEmpty())
		{
			events = (IQueryable<Event>)matchingPractices;
		}
		else if (matchingPractices.IsNullOrEmpty() && !matchingGames.IsNullOrEmpty())
		{
			events = (IQueryable<Event>)matchingGames;
		}
		else if (!matchingGames.IsNullOrEmpty() && !matchingPractices.IsNullOrEmpty())
		{
			events = matchingPractices.Concat((IQueryable<Event>)matchingGames).AsQueryable();
		}

		ViewData["TeamFilterMessage"] = "for Team " + selectedTeam.Name;

		return events;
	}

	/// <summary>
	/// Builds a list of Events using a search term.
	/// </summary>
	/// <param name="searchTerm">The inputted search term.</param>
	/// <param name="type">The currently selected Event type - defaults to Event.</param>
	/// <param name="events">A list of Events - defaults to null.</param>
	/// <returns>A filtered list of Events.</returns>
	public IQueryable<Event> nameSearch(string searchTerm, string? type = "Event", IQueryable<Event>? events = null)
	{
		if(events.IsNullOrEmpty())
		{
			events = this.context.Events
					.WithScheduling();
		}
		events = events.Where(e => e.Name.ToLower().Contains(searchTerm.ToLower()));

		if (events.IsNullOrEmpty())
		{
			ViewData["NameFilterMessage"] = "There are no " + type + "s that match the search term " + searchTerm;
		}
		else
		{
			ViewData["NameFilterMessage"] = "that match the search term " + searchTerm;
		}

		return events;
	}
}
