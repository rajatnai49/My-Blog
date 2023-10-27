using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using aspnet_blog_application.Models;
using aspnet_blog_application.Models.ViewModels;
using Microsoft.Data.Sqlite;
using System.Security.Claims;

namespace aspnet_blog_application.Controllers;

public class PostsController : Controller
{
    private readonly ILogger<PostsController> _logger;

    private readonly IConfiguration _configuration;

    public PostsController(ILogger<PostsController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public IActionResult Index()
    {
        // Get the user's ID from the authentication cookie
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId != null)
        {
            // Convert the user ID to an integer (assuming it's an integer)
            if (int.TryParse(userId, out int parsedUserId))
            {
                var postListViewModel = new PostViewModel
                {
                    PostList = GetPostsByUserId(parsedUserId)
                };
                return View(postListViewModel);
            }
        }

        // Handle the case when the user is not authenticated or the ID cannot be parsed
        // You might want to redirect to a login page or display an error message.
        return RedirectToAction("Login", "User");
    }

    public async Task<IActionResult> AllPosts()
    {
        var postListViewModel = GetAllPosts();

        return View(postListViewModel);
    }



    internal List<PostModel> GetPostsByUserId(int userId)
    {
        List<PostModel> posts = new List<PostModel>();

        using (var connection = new SqliteConnection(_configuration.GetConnectionString("BlogDataContext")))
        {
            using (var command = connection.CreateCommand())
            {
                connection.Open();
                command.CommandText = $"SELECT * FROM post WHERE user = '{userId}'";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var post = new PostModel
                        {
                            Id = reader.GetInt32(0),
                            Title = reader.GetString(1),
                            Body = reader.GetString(2),
                            // You may need to adjust this part based on your database schema
                            CreatedAt = reader.GetDateTime(3),
                            UpdatedAt = reader.GetDateTime(4),
                            UserId = reader.GetInt32(5)  // Assuming UserId is the index of the UserId column
                        };

                        posts.Add(post);
                    }
                }
            }
        }

        return posts;
    }


    public IActionResult NewPost()
    {
        return View();
    }

    public IActionResult EditPost(int id) 
    {
        var post = GetPostById(id);
        var postViewModel = new PostViewModel();
        postViewModel.Post = post;
        return View(postViewModel);
    }

    public IActionResult ViewPost(int id) 
    {
        var post = GetPostById(id);
        var postViewModel = new PostViewModel();
        postViewModel.Post = post;
        return View(postViewModel);
    }

    internal PostModel GetPostById(int id)
    {
        PostModel post = new();

        using (var connection = new SqliteConnection(_configuration.GetConnectionString("BlogDataContext")))
        {
            using (var command = connection.CreateCommand())
            {
                connection.Open();
                command.CommandText = $"SELECT * FROM post Where Id = '{id}'";

                using (var reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        reader.Read();
                        post.Id = reader.GetInt32(0);
                        post.Title = reader.GetString(1);
                        post.Body = reader.GetString(2);
                    }
                    else
                    {
                        return post;
                    }
                };
            }
        }

        return post;
    }

    internal PostViewModel GetAllPosts()
    {
        List<PostModel> postList = new();

        using (SqliteConnection connection = new SqliteConnection(_configuration.GetConnectionString("BlogDataContext")))
        {
            using (var command = connection.CreateCommand())
            {
                connection.Open();
                command.CommandText = "SELECT * FROM post";

                using (var reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            postList.Add(
                                new PostModel
                                {
                                    Id = reader.GetInt32(0),
                                    Title = reader.GetString(1),
                                    Body = reader.GetString(2),
                                });
                        }
                    }
                    else
                    {
                        return new PostViewModel
                        {
                            PostList = postList
                        };
                    }
                };
            }
        }

        return new PostViewModel
        {
            PostList = postList
        };
    }

    public ActionResult Insert(PostModel post)
    {
        post.CreatedAt = DateTime.Now;
        post.UpdatedAt = DateTime.Now;

        // Get the user's ID from the authentication cookie
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId != null && int.TryParse(userId, out int parsedUserId))
        {
            post.UserId = parsedUserId; // Set the user's ID in the PostModel

            using (SqliteConnection connection = new SqliteConnection(_configuration.GetConnectionString("BlogDataContext")))
            {
                using (var command = connection.CreateCommand())
                {
                    connection.Open();
                    command.CommandText = "INSERT INTO post (Title, Body, CreatedAt, UpdatedAt, user) VALUES " +
                                          "(@Title, @Body, @CreatedAt, @UpdatedAt, @UserId)";

                    // Use parameters to prevent SQL injection
                    command.Parameters.AddWithValue("@Title", post.Title);
                    command.Parameters.AddWithValue("@Body", post.Body);
                    command.Parameters.AddWithValue("@CreatedAt", post.CreatedAt);
                    command.Parameters.AddWithValue("@UpdatedAt", post.UpdatedAt);
                    command.Parameters.AddWithValue("@UserId", post.UserId);

                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        // Handle the exception as needed
                    }
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // Handle the case when the user is not authenticated or the ID cannot be parsed
        // You might want to redirect to a login page or display an error message.
        return RedirectToAction("Login", "User");
    }


    public ActionResult Update(PostModel post)
    {
        post.UpdatedAt = DateTime.Now;

        using (SqliteConnection connection = new SqliteConnection(_configuration.GetConnectionString("BlogDataContext")))
        {
            using (var command = connection.CreateCommand())
            {
                connection.Open();

                // Use parameterized query to avoid SQL injection
                command.CommandText = "UPDATE post SET Title = @Title, Body = @Body, UpdatedAt = @UpdatedAt WHERE Id = @Id";
                command.Parameters.AddWithValue("@Title", post.Title);
                command.Parameters.AddWithValue("@Body", post.Body);
                command.Parameters.AddWithValue("@UpdatedAt", post.UpdatedAt);
                command.Parameters.AddWithValue("@Id", post.Id);

                try
                {
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public JsonResult Delete(int id)
    {
        using (SqliteConnection connection =
                new SqliteConnection(_configuration.GetConnectionString("BlogDataContext")))
        {
            using (var command = connection.CreateCommand())
            {
                connection.Open();
                command.CommandText = $"DELETE from post WHERE Id = '{id}'";
                command.ExecuteNonQuery();
            }
        }

        return Json(new Object{});
    }



    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
