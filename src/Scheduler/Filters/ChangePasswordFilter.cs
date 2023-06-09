﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Scheduler.Domain.Models;
using Scheduler.Web.Controllers;
using System.Reflection;

namespace Scheduler.Filters;

public sealed class ChangePasswordFilter : IAuthorizationFilter
{
	private readonly UserManager<User> userManager;

	public ChangePasswordFilter(UserManager<User> userManager)
	{
		this.userManager = userManager;
	}

	public void OnAuthorization(AuthorizationFilterContext filterContext)
	{
		if (filterContext.HttpContext.User.Identity is not null &&
			filterContext.HttpContext.User.Identity.IsAuthenticated)
		{
			User user = this.userManager.GetUserAsync(filterContext.HttpContext.User).Result
				?? throw new NullReferenceException("Could not determine current user.");

			bool skip = SkipFilter(in filterContext);

			if (user.NeedsNewPassword && !skip)
			{
				filterContext.Result = new RedirectToActionResult(
					nameof(IdentityController.ForceReset),
					"Identity", null);
			}
		}
	}

	private static bool SkipFilter(in AuthorizationFilterContext context)
	{
		var descriptor = context.ActionDescriptor as ControllerActionDescriptor ??
			throw new NullReferenceException("Could not determine HTTP request.");

		IEnumerable<CustomAttributeData> attributes = descriptor.MethodInfo.CustomAttributes;
		bool skip = attributes.Any(a => a.AttributeType == typeof(IgnoreChangePasswordAttribute));

		return skip;
	}
}
