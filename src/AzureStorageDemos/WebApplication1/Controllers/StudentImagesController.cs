using Microsoft.AspNetCore.Mvc;

using WebApplication1.Services;
using WebApplication1.ViewModels;

namespace WebApplication1.Controllers;


public class StudentImagesController : Controller
{
    private readonly MyImagesBlobClientService _blobService;
    private readonly MyQueueClientService _queueService;

    public StudentImagesController(
        MyImagesBlobClientService blobService,
        MyQueueClientService queueService)
    {
        _blobService = blobService;
        _queueService = queueService;
    }


    public IActionResult Index()
    {
        var vm = new StudentImageUploadViewModel();

        return View(vm);
    }


    [HttpPost]
    public async Task<IActionResult> Index(StudentImageUploadViewModel vm)
    {
        if(! ModelState.IsValid)
        {
            return View(vm);
        }

        var imageFileName = await _blobService.UploadAsync(vm.ImageFile!);

        // Add the name of the image to the queue for asynchronous process by the Azure Function!
        await _queueService.SendMessageAsync(imageFileName);

        vm.ImageFileName = imageFileName;
        return View(vm);
    }


    [HttpGet]
    public async Task<IActionResult> GetImage(string fileName)
    {
        var (stream, contentType) = await _blobService.GetBlobItemAsync(fileName);

        return File(stream, contentType, fileDownloadName: fileName);
    }


    [HttpGet]
    public async Task<IActionResult> List()
    {
        var blobItems = await _blobService.GetAllAsync();
        return View(blobItems);
    }


    [HttpGet]
    public async Task<IActionResult> Delete(string fileName)
    {
        await _blobService.DeleteAsync(fileName);
        return RedirectToAction("List");
    }

}
