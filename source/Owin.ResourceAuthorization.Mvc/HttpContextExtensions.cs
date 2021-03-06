﻿using Microsoft.Owin;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Thinktecture.IdentityModel;
using Thinktecture.IdentityModel.Owin.ResourceAuthorization;

namespace System.Web
{
    public static class HttpContextExtensions
    {
        public static bool CheckAccess(this HttpContextBase httpContext, string action, params string[] resources)
        {
            return AsyncHelper.RunSync(() => httpContext.CheckAccessAsync(action, resources));
        }

        public static Task<bool> CheckAccessAsync(this HttpContextBase httpContext, string action, params string[] resources)
        {
            var cp = httpContext.User as ClaimsPrincipal;
            var authorizationContext = new ResourceAuthorizationContext(
                cp ?? Principal.Anonymous,
                action,
                resources);

            return httpContext.CheckAccessAsync(authorizationContext);
        }

        public static Task<bool> CheckAccessAsync(this HttpContextBase httpContext, IEnumerable<Claim> actions, IEnumerable<Claim> resources)
        {
            var cp = httpContext.User as ClaimsPrincipal;
            var authorizationContext = new ResourceAuthorizationContext(
                cp ?? Principal.Anonymous,
                actions,
                resources);

            return httpContext.CheckAccessAsync(authorizationContext);
        }

        public static Task<bool> CheckAccessAsync(this HttpContextBase httpContext, ResourceAuthorizationContext authorizationContext)
        {
            return httpContext.GetOwinContext().CheckAccessAsync(authorizationContext);
        }

        private static async Task<bool> CheckAccessAsync(this IOwinContext context, ResourceAuthorizationContext authorizationContext)
        {
            return await context.GetAuthorizationManager().CheckAccessAsync(authorizationContext);
        }

        private static IResourceAuthorizationManager GetAuthorizationManager(this IOwinContext context)
        {
            var am = context.Get<IResourceAuthorizationManager>(ResourceAuthorizationManagerMiddleware.Key);

            if (am == null)
            {
                throw new InvalidOperationException("No AuthorizationManager set.");
            }

            return am;
        }
    }

    internal static class AsyncHelper
    {
        private static readonly TaskFactory _myTaskFactory = new TaskFactory(CancellationToken.None, TaskCreationOptions.None, TaskContinuationOptions.None, TaskScheduler.Default);

        public static void RunSync(Func<Task> func)
        {
            _myTaskFactory.StartNew<Task>(func).Unwrap().GetAwaiter().GetResult();
        }

        public static TResult RunSync<TResult>(Func<Task<TResult>> func)
        {
            return _myTaskFactory.StartNew<Task<TResult>>(func).Unwrap<TResult>().GetAwaiter().GetResult();
        }
    }
}