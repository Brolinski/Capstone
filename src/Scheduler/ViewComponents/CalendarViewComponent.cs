﻿using Microsoft.AspNetCore.Mvc;
using Scheduler.Domain.Models;
using Scheduler.ViewModels;
using Scheduler.Domain.Repositories;
using Scheduler.Domain.Specifications;
using Scheduler.Domain.Specifications.Events;
using Scheduler.Domain.Services;

namespace Scheduler.ViewComponents;

public sealed class CalendarViewComponent : ViewComponent
{
	/// <summary>
	/// The repository to query events with.
	/// </summary>
	private readonly IScheduleRepository scheduleRepository;

	/// <summary>
	/// API for retrieving dates.
	/// </summary>
	private readonly IDateProvider dateProvider;

	/// <summary>
	/// Initializes the <see cref="CalendarViewComponent"/> class.
	/// </summary>
	/// <param name="scheduleRepository">The repository to query events with.</param>
	/// <param name="dateProvider">API for retrieving dates.</param>
	public CalendarViewComponent(
		IScheduleRepository scheduleRepository,
		IDateProvider dateProvider)
	{
		this.scheduleRepository = scheduleRepository;
		this.dateProvider = dateProvider;
	}

	public async Task<IViewComponentResult> InvokeAsync(int? selectedYear = null, int? selectedMonth = null)
	{
		Specification<Event> pastEventsSpec = new PastEventSpecification(this.dateProvider);
		IEnumerable<Event> events = await this.scheduleRepository.SearchAsync(
			pastEventsSpec.Not());

		int currentYear;
		int currentMonth;

		if (this.ViewData["Year"] is not null)
		{
			currentYear = Convert.ToInt32(this.ViewData["Year"]);
			currentMonth = Convert.ToInt32(this.ViewData["Month"]);
		}
		else
		{
			currentYear = this.dateProvider.Now.Year;
			currentMonth = this.dateProvider.Now.Month;
		}

		DateTime firstOfMonth;
		DateTime lastOfMonth;
		DateTime currentDay;
		DateTime topOfCalendar;
		DateTime bottomOfCalendar;

		firstOfMonth = new DateTime(currentYear, currentMonth, 1);
		lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1);
		topOfCalendar = GetTopOfCalendar(firstOfMonth);
		bottomOfCalendar = GetBottomOfCalendar(lastOfMonth);
		currentDay = this.dateProvider.Now.Date;

		ViewData["CurrentYear"] = currentYear;

		if (currentMonth == this.dateProvider.Now.Month && currentYear == this.dateProvider.Now.Year)
		{
			this.ViewData["PreviousMonth"] = 0;
			this.ViewData["PreviousYear"] = 0;
		}
		else
		{
			if (currentMonth != 1)
			{
				this.ViewData["PreviousMonth"] = currentMonth - 1;
				this.ViewData["PreviousYear"] = currentYear;
			}
			else
			{
				this.ViewData["PreviousMonth"] = 12;
				this.ViewData["PreviousYear"] = currentYear - 1;
			}
		}

		if (currentMonth != 12)
		{
			this.ViewData["NextMonth"] = currentMonth + 1;
			this.ViewData["NextYear"] = currentYear;
		}
		else
		{
			this.ViewData["NextMonth"] = 1;
			this.ViewData["NextYear"] = currentYear + 1;
		}

		this.ViewData["MonthName"] = ((Month)currentMonth - 1).ToString();
		this.ViewData["MonthDateStart"] = firstOfMonth;
		this.ViewData["MonthDateEnd"] = firstOfMonth.AddMonths(1).AddDays(-1);
		this.ViewData["CurrentDay"] = currentDay;
		this.ViewData["Month"] = currentMonth;
		this.ViewData["Year"] = currentYear;

		BuildCalendarDays(topOfCalendar, bottomOfCalendar, currentDay, firstOfMonth, lastOfMonth, currentMonth, events);

		return await Task.FromResult((IViewComponentResult)View("Calendar"));
	}

	private DateTime GetTopOfCalendar(DateTime firstOfMonth)
	{
		return FirstDayOfWeek(firstOfMonth);
	}

	private DateTime GetBottomOfCalendar(DateTime lastOfMonth)
	{
		return FirstDayOfWeek(lastOfMonth).AddDays(6);
	}

	private DateTime FirstDayOfWeek(DateTime dt)
	{
		var culture = Thread.CurrentThread.CurrentCulture;
		var diff = dt.DayOfWeek - culture.DateTimeFormat.FirstDayOfWeek;

		if (diff < 0)
		{
			diff += 7;
		}

		return dt.AddDays(-diff).Date;
	}

	private void BuildCalendarDays(DateTime topOfCalendar, DateTime bottomOfCalendar, DateTime currentDay, DateTime firstOfMonth, DateTime lastOfMonth, int currentMonth, IEnumerable<Event> events)
	{
		var weeklyList = new List<string>();

		DateTime daypart = topOfCalendar;

		int i = 1;

		int week = 0;

		while (daypart < bottomOfCalendar.AddDays(1))
		{
			// Week Date Range
			if (i == 1)
			{
				week++;
				ViewData["Week" + week + "Start"] = daypart;
				ViewData["Week" + week + "End"] = daypart.AddDays(6);
			}

			// Button Date (full date)
			weeklyList.Add(daypart.ToString());

			// Calendar Date (day of month)
			weeklyList.Add(daypart.Day.ToString());

			// Current Day (current/notcurrent)
			if (daypart.Date == currentDay.Date)
			{
				weeklyList.Add("current");
			}
			else
			{
				weeklyList.Add("notcurrent");
			}

			// Show Events (active/inactive), Past Date CSS Class (grey/normal)
			if (daypart < currentDay)
			{
				weeklyList.Add("inactive");
				weeklyList.Add("grey");
			}
			else
			{
				weeklyList.Add("active");
				weeklyList.Add("normal");
			}

			// Previous/Next Month CSS Class
			if (daypart < firstOfMonth || daypart > lastOfMonth)
			{
				weeklyList.Add("brown");
			}
			else
			{
				weeklyList.Add("normal");
			}

			// Counting Events
			int eventCount = 0;

			foreach (Event e in events)
			{
				if (e.StartDate.Date == daypart.Date || (e.StartDate.Date < daypart.Date && e.EndDate.Date >= daypart.Date))
				{
					eventCount++;
				}
			}

			weeklyList.Add(eventCount.ToString());

			i++;

			if (i == 8)
			{
				ViewData["Week" + week + "List"] = weeklyList;

				weeklyList = new List<string>();

				i = 1;
			}

			daypart = daypart.AddDays(1);
		}

		ViewData["NumberOfWeeks"] = week;
	}
}
