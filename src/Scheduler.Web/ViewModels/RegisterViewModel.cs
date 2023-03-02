﻿using System.ComponentModel.DataAnnotations;
using Scheduler.Core.Models.Identity;
using Scheduler.Web.Controllers;

namespace Scheduler.Web.ViewModels;

/// <summary>
/// Form data for <see cref="IdentityController.Register"/> POST.
/// </summary>
public sealed record RegisterViewModel
{
	/// <summary>
	/// User credentials.
	/// </summary>
	public required Credentials Credentials { get; init; }

	/// <summary>
	/// Used for comparison with <see cref="ConfirmPassword"/>.
	/// </summary>
	public string Password
		=> this.Credentials.Password;

	/// <summary>
	/// Compared against <see cref="Password"/>.
	/// </summary>
	[Required(ErrorMessage = "Please confirm your password.")]
	[DataType(DataType.Password)]
	[Display(Name = "Confirm password")]
	[Compare(nameof(Password), ErrorMessage = "The password and confirmation password do not match.")]
	public string ConfirmPassword { get; init; } = string.Empty;


	[Required(ErrorMessage = "Please enter a first name.")]
	[MaxLength(32)]
	public string FirstName { get; init; } = string.Empty;

	[Required(ErrorMessage = "Please enter a last name.")]
	[MaxLength(32)]
	public string LastName { get; init; } = string.Empty;

	public bool IsAdmin { get; init; } = false;
}
