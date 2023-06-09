﻿using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Scheduler.Domain.Models;

/// <summary>
/// Represents a user in the scheduler.
/// </summary>
/// <remarks>
/// Without this class, <see cref="IdentityUser{TKey}"/> would need to be referenced everywhere.
/// </remarks>
public sealed class User : IdentityUser<Guid>
{
	/// <summary>
	/// The users's first name.
	/// </summary>
	[Required]
	[MaxLength(32)]
	public required string FirstName { get; set; }

	/// <summary>
	/// The user's last name.
	/// </summary>
	[Required]
	[MaxLength(32)]
	public required string LastName { get; set; }

	/// <summary>
	/// Determines whether the user needs to reset their password.
	/// </summary>
	public bool NeedsNewPassword { get; set; }

	/// <inheritdoc/>
	public override string ToString()
	{
		return $"{this.FirstName} {this.LastName}";
	}
}
