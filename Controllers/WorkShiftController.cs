﻿using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MiniStore.Context;
using MiniStore.Models;
using MiniStore.ViewModels;

namespace MiniStore.Controllers
{
    [Route("api/work-shift")]
    [ApiController]
    public class WorkShiftController : ControllerBase
    {
        private readonly MiniStoreContext _context;

        public WorkShiftController(MiniStoreContext context)
        {
            _context = context;
        }

        [EnableCors("Default")]
        [HttpPost("")]
        public async Task<IActionResult> RegisterWorkShift(RegisterWorkshift registerWorkshift)
        {
            List<WorkShift> workShifts = registerWorkshift.Workshifts
                .Select(ws => new WorkShift
                {
                    Id = Utility.Ultility.GenerateEightDigitId(),
                    EmployeeId = registerWorkshift.EmployeeId,
                    ApprovalStatus = _context.ApprovalStatuses.FirstOrDefault(ap => ap.Id.Equals(1))!,
                    StartDate = ws.StartDate,
                    EndDate = ws.EndDate,
                    WorkshiftType = _context.WorkshiftsType.FirstOrDefault(wst => wst.Name.Equals(ws.WorkshiftType))!,
                })
                .ToList();

            foreach (WorkShift ws in workShifts)
            {
                DateTime tmp = ws.StartDate;
                DateTime tmp2 = ws.EndDate;

                switch (ws.WorkshiftType.Name)
                {
                    case "saler-shift-1":

                        ws.StartDate = new DateTime(tmp.Year, tmp.Month, tmp.Day, 6, 0, 0);
                        ws.EndDate = new DateTime(tmp.Year, tmp.Month, tmp.Day, 12, 0, 0);
                        break;
                    case "saler-shift-2":
                        ws.StartDate = new DateTime(tmp.Year, tmp.Month, tmp.Day, 12, 0, 0);
                        ws.EndDate = new DateTime(tmp.Year, tmp.Month, tmp.Day, 18, 0, 0);
                        break;
                    case "saler-shift-3":
                        ws.StartDate = new DateTime(tmp.Year, tmp.Month, tmp.Day, 18, 0, 0);
                        tmp2 = tmp2.AddDays(1);
                        ws.EndDate = new DateTime(tmp2.Year, tmp2.Month, tmp2.Day, 6, 0, 0);

                        if (ws.StartDate.DayOfWeek == DayOfWeek.Sunday)
                        {
                            ws.CoefficientsSalary = 2;
                        }
                        else if (ws.IsHoliday)
                        {
                            ws.CoefficientsSalary = 3;
                        }
                        else
                        {
                            ws.CoefficientsSalary = (decimal)1.5;
                        }

                        break;
                    case "guard-shift-1":
                        ws.StartDate = new DateTime(tmp.Year, tmp.Month, tmp.Day, 6, 0, 0);
                        ws.EndDate = new DateTime(tmp.Year, tmp.Month, tmp.Day, 18, 0, 0);
                        break;
                    case "guard-shift-2":
                        ws.StartDate = new DateTime(tmp.Year, tmp.Month, tmp.Day, 18, 0, 0);
                        tmp2 = tmp2.AddDays(1);
                        ws.EndDate = new DateTime(tmp2.Year, tmp2.Month, tmp2.Day, 6, 0, 0);
                        ws.CoefficientsSalary = (decimal)1.5;
                        break;
                    default:
                        break;
                }
            }

            foreach (WorkShift ws in workShifts)
            {
                bool check = _context.WorkShifts
                .Where(ws1 => ws1.EmployeeId.Equals(ws.EmployeeId))
                .Any(ws1 => ws1.StartDate.DayOfYear.Equals(ws.StartDate.DayOfYear) && ws1.WorkshiftTypeId.Equals(ws.WorkshiftType.Id));

                if (!check)
                {
                    await _context.WorkShifts.AddAsync(ws);
                }
            }

            await _context.SaveChangesAsync();
            return Ok(workShifts);
        }

        [EnableCors("Default")]
        [HttpGet("{employeeId}")]
        public async Task<ActionResult<WorkShift>> ShowWorkshiftsByEmployeeId(string employeeId, DateOnly startDate, DateOnly endDate)
        {
            var registerWorkshift = await _context.WorkShifts
                .Where(ws => ws.EmployeeId.Equals(employeeId))
                .Where(ws => ws.StartDate.DayOfYear >= startDate.DayOfYear && ws.EndDate.DayOfYear <= endDate.DayOfYear)
                .ToListAsync();

            return Ok(registerWorkshift);
        }

        [EnableCors("Default")]
        [HttpGet("")]
        public async Task<ActionResult<IEnumerable<WorkShift>>> ShowAllWorkshifts(DateOnly startDate, DateOnly endDate)
        {
            var allWorkshifts = await _context.WorkShifts
                .Where(ws => ws.StartDate.DayOfYear >= startDate.DayOfYear && ws.EndDate.DayOfYear <= endDate.DayOfYear)
                .ToListAsync();

            allWorkshifts.ForEach(ws =>
            {
                ws.Employee = _context.Employees.FirstOrDefault(emp => emp.Id.Equals(ws.EmployeeId))!;
            });

            if (allWorkshifts.IsNullOrEmpty()) return NoContent();

            return Ok(allWorkshifts);
        }

        [EnableCors("Default")]
        [HttpPut("confirm")]
        public async Task<IActionResult> ConfirmWorkshift(ViewConfirmWorkshift confirm)
        {
            if (confirm is not ViewConfirmWorkshift) return BadRequest(new { Message = "Please give correct format" });

            var workshift = await _context.WorkShifts
                .Where(ws => ws.Id.Equals(confirm.WorkshiftId))
                .Where(ws => ws.EmployeeId.Equals(confirm.EmployeeId))
                .FirstOrDefaultAsync();

            if (workshift == null) return NotFound();

            workshift.ApprovalStatusId = (confirm.IsConfirm) ? 2 : 3; //Id 2 is approve and 3 is reject

            _context.Update(workshift);
            await _context.SaveChangesAsync();
            return Ok(workshift);
        }

    }
}
