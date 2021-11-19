﻿using Ardalis.ApiEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Sparc.Features
{
    public class HttpResponseException : Exception
    {
        public HttpResponseException(string? message) : base(message)
        {
        }

        public virtual HttpStatusCode Status { get; set; } = HttpStatusCode.InternalServerError;
    }
    
    public class NotFoundException : HttpResponseException
    {
        public NotFoundException(string? message) : base(message)
        {
        }

        public override HttpStatusCode Status { get; set; } = HttpStatusCode.NotFound;
    }

    public class NotAuthorizedException : HttpResponseException
    {
        public NotAuthorizedException(string? message) : base(message)
        {
        }

        public override HttpStatusCode Status { get; set; } = HttpStatusCode.Unauthorized;
    }

    public class ForbiddenException : HttpResponseException
    {
        public ForbiddenException(string? message) : base(message)
        {
        }

        public override HttpStatusCode Status { get; set; } = HttpStatusCode.Forbidden;
    }

    public static class FeatureExtensions
    {
        public static ActionResult Exception(this BaseEndpointAsync controller, HttpResponseException exception)
        {
            return exception.Status switch
            {
                HttpStatusCode.Unauthorized => controller.Unauthorized(exception.Message),
                HttpStatusCode.NotFound => controller.NotFound(exception.Message),
                HttpStatusCode.Forbidden => controller.Forbid(exception.Message),
                _ => controller.Problem(exception.Message, statusCode: (int)exception.Status),
            };
        }
    }
}
