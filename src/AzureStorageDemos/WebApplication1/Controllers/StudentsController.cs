using Microsoft.AspNetCore.Mvc;

using WebApplication1.Entities;
using WebApplication1.Services;
using WebApplication1.ViewModels;

namespace WebApplication1.Controllers;

public class StudentsController : Controller
{
    private readonly MyStudentTableClientService _service;

    public StudentsController(MyStudentTableClientService service)
    {
        _service = service;
    }


    // READ (List)
    public async Task<IActionResult> Index()
    {
        var data = await _service.GetAllStudentsAsync();

        var vm = data.Select(x => new StudentViewModel {
            RowKey = x.RowKey,
            Name = x.Name,
            Email = x.Email,
            Age = x.Age
        }).ToList();

        return View(vm);
    }


    // CREATE (GET)
    public IActionResult Create()
    {
        return View(viewName: "CreateEdit", model: new StudentViewModel());
    }


    // CREATE (POST)
    [HttpPost]
    public async Task<IActionResult> Create(StudentViewModel vm)
    {
        if( !ModelState.IsValid)
        {
            return View(viewName: "CreateEdit", model: vm);
        }

        var student = new StudentEntity
        {
            Name = vm.Name,
            Email = vm.Email,
            Age = vm.Age
        };

        await _service.AddStudentAsync(student);

        return RedirectToAction(nameof(Index));
   }


    // EDIT (GET)
    public async Task<IActionResult> Edit(string rowKey)
    {
        var student = await _service.GetStudentAsync(rowKey);

        if(student is null)
        {
            return NotFound();
        }

        var vm = new StudentViewModel
        {
            RowKey = student.RowKey,
            Name = student.Name,
            Email = student.Email,
            Age =student.Age
        };

        return View(viewName: "CreateEdit", model: vm);
    }


    // EDIT (POST)
    [HttpPost]
    public async Task<IActionResult> Edit(StudentViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            return View(viewName: "CreateEdit", model: vm);
        }

        var student = new StudentEntity
        {
            PartitionKey = StudentEntity.TableNAME,
            RowKey = vm.RowKey!,
            ETag = Azure.ETag.All,

            Name = vm.Name,
            Email = vm.Email,
            Age = vm.Age
        };

        await _service.UpdateStudentAsync(student);

        return RedirectToAction(nameof(Index));
    }


    // DELETE
    public async Task<IActionResult> Delete(string rowKey)
    {
        await _service.DeleteStudentAsync(rowKey);

        return RedirectToAction(nameof(Index));
    }

}