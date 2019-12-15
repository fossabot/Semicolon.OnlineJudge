﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Markdig;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Semicolon.OnlineJudge.Data;
using Semicolon.OnlineJudge.Models.Problemset;
using Semicolon.OnlineJudge.Models.User;
using Semicolon.OnlineJudge.Models.ViewModels.Problemset;

namespace Semicolon.OnlineJudge.Controllers
{
    public class ProblemsetController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProblemsetController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var model = new IndexModel
            {
                ProblemModels = new List<ProblemModel>()
            };
            foreach (var p in _context.Problems.ToList())
            {
                var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().UseBootstrap().Build();

                var html = Markdown.ToHtml(p.Description, pipeline);
                var raw = Markdown.ToPlainText(p.Description);

                var author = _context.OJUsers.FirstOrDefault(x => x.Id == p.AuthorId);

                model.ProblemModels.Add(new ProblemModel
                {
                    Id = p.Id,
                    Title = p.Title,
                    Description = p.Description,
                    ContentRaw = raw,
                    ContentHtml = html,
                    AuthorId = p.AuthorId,
                    Author = author.UserName,
                    ExampleData = p.ExampleData,
                    JudgeProfile = p.JudgeProfile,
                    PassRate = p.PassRate,
                    PublishTime = p.PublishTime
                });
            }

            return View(model);
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
        public IActionResult New()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
        public IActionResult New(NewModel model)
        {
            if (ModelState.IsValid)
            {
                return RedirectToAction(nameof(NewTestData), model);
            }

            return View();
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
        public IActionResult NewTestData(NewModel model)
        {
            model.TestDatas = new List<Models.Problemset.TestData>();
            for (int i = 0; i < model.TestDataNumber; i++)
            {
                model.TestDatas.Add(new Models.Problemset.TestData { Input = "Your data", Output = "Your data" });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
        public async Task<IActionResult> NewTestData(NewModel model, string status)
        {
            var user = GetUserProfile();
            if(user == null)
            {
                return Unauthorized();
            }

            var problem = new Problem
            {
                Title = model.Title,
                Description = model.Description,
                AuthorId = user.Id,
                PublishTime = DateTime.UtcNow,
            };

            var judgeProfile = new JudgeProfile
            {
                MemoryLimit = model.MemoryLimit,
                TimeLimit = model.TimeLimit,
                TestDatas = string.Empty
            };
            judgeProfile.SetTestDatas(model.TestDatas);
            problem.SetJudgeProfile(judgeProfile);

            problem.SetPassRate(new PassRate
            {
                Submit = 0,
                Pass = 0
            });

            _context.Problems.Add(problem);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Route("{id}")]
        public IActionResult Details(long? id)
        {
            var problem = _context.Problems.FirstOrDefault(p => p.Id == id);

            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().UseBootstrap().Build();

            var html = Markdown.ToHtml(problem.Description, pipeline);
            var raw = Markdown.ToPlainText(problem.Description);

            var author = _context.OJUsers.FirstOrDefault(x => x.Id == problem.AuthorId);

            var model = new ProblemModel
            {
                Id = problem.Id,
                Title = problem.Title,
                Description = problem.Description,
                ContentRaw = raw,
                ContentHtml = html,
                AuthorId = problem.AuthorId,
                Author = author.UserName,
                ExampleData = problem.ExampleData,
                JudgeProfile = problem.JudgeProfile,
                PassRate = problem.PassRate,
                PublishTime = problem.PublishTime
            };

            return View(model);
        }

        private OJUser GetUserProfile()
        {
            var user = new OJUser();

            try
            {
                var id = HttpContext.User.FindFirst("UserId").Value;
                user = _context.OJUsers.FirstOrDefault(u => u.Id == id);
            }
            catch
            {
                return null;
            }

            return user;
        }
    }
}