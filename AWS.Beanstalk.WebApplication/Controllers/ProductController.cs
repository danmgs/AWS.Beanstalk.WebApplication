using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Util;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Handlers.SqlServer;
using Amazon.XRay.Recorder.Handlers.System.Net;
using AWS.Beanstalk.WebApplication.ActionFilters;
using AWS.Beanstalk.WebApplication.Models;
using AWS.Beanstalk.WebApplication.Utils;
using log4net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace AWS.Beanstalk.WebApplication.Controllers
{
    //[XRayActionFilter] TODO: not working: x-ray traces will be missing.
    public class ProductController : Controller
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ProductController));

        private readonly Lazy<AmazonDynamoDBClient> LazyDdbClient;

        private readonly Lazy<Table> LazyTable;

        private IConfiguration _configuration;

        public ProductController(IConfiguration configuration)
        {
            _configuration = configuration;
            string tableName = _configuration.GetValue<string>("TableName");
            string region = _configuration.GetValue<string>("Region");
            LazyDdbClient = new Lazy<AmazonDynamoDBClient>(() =>
            {
                RegionEndpoint r;
                try
                {
                    r = EC2InstanceMetadata.Region;
                }
                catch (Exception)
                {
                    // when running locally, the region is configured in appsettings.json.
                    r = RegionEndpoint.EnumerableAllRegions.Where(reg => reg.SystemName.Equals(region)).FirstOrDefault();
                }

                _log.Info($"AWS region is {r}");
                var client = new AmazonDynamoDBClient(r);

                return client;
            });

            LazyTable = new Lazy<Table>(() =>
            {
                return Table.LoadTable(LazyDdbClient.Value, tableName);
            });
        }

        // GET: Product
        public ActionResult<IEnumerable<Product>> Index()
        {
            IEnumerable<Product> list = new List<Product>();

            list = AWSXRayRecorder.Instance.TraceMethod<IEnumerable<Product>>("ScanAllProducts", () => ScanAllProducts());

            return View(list);
        }

        // GET: Product/Details/5
        public ActionResult Details(string id)
        {
            try
            {
                var product = AWSXRayRecorder.Instance.TraceMethod<Product>("DetailsProduct", () => DetailsProduct(id));

                // Trace out-going HTTP request
                AWSXRayRecorder.Instance.TraceMethod("Outgoing Http Web Request", () => MakeDummyHttpWebRequest(id));

                CustomSubsegment("we are inside method: DetailsProduct", 500);

                // Trace SQL query
                //  AWSXRayRecorder.Instance.TraceMethod("Query SQL", () => QuerySql(id));

                return View(product);
            }
            catch (ProductNotFoundException)
            {
                throw;
                //return "Product not found !";// NotFound();
            }
        }

        // GET: Product/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Default/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                var p = new Product()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = collection["Name"],
                    Price = Convert.ToDecimal(collection["Price"])
                };

                AWSXRayRecorder.Instance.TraceMethodAsync("CreateProduct", () => CreateProduct<Document>(p));

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: Product/Edit/5
        public ActionResult Edit(string id)
        {
            Product p = DetailsProduct(id);
            return View(p);
        }

        // POST: Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(string id, IFormCollection collection)
        {
            try
            {
                var p = new Product()
                {
                    Id = id,
                    Name = collection["Name"],
                    Price = Convert.ToDecimal(collection["Price"])
                };

                AWSXRayRecorder.Instance.TraceMethodAsync("EditProduct", () => EditProduct<Document>(p));

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: Product/Delete/5
        public ActionResult Delete(string id)
        {
            AWSXRayRecorder.Instance.TraceMethodAsync("DeleteProduct", () => DeleteProduct<Document>(id));
            return RedirectToAction(nameof(Index));
        }

        // Create custom Subsegment
        private void CustomSubsegment(string label, int sleepsmilliseconds)
        {
            try
            {
                AWSXRayRecorder.Instance.BeginSubsegment(label);

                // Add business some logic
                Thread.Sleep(sleepsmilliseconds);

            }
            catch (Exception e)
            {
                AWSXRayRecorder.Instance.AddException(e);
            }
            finally
            {
                AWSXRayRecorder.Instance.EndSubsegment();
            }
        }

        private static void MakeDummyHttpWebRequest(string id)
        {
            string websiteUrl = "http://www.amazon.com";
            AWSXRayRecorder.Instance.AddAnnotation("productId", id);
            AWSXRayRecorder.Instance.AddAnnotation("websiteUrl", websiteUrl);
            AWSXRayRecorder.Instance.AddAnnotation("operationType", "MakeDummyHttpWebRequest");
            HttpWebRequest request = null;
            request = (HttpWebRequest)WebRequest.Create(websiteUrl);
            request.GetResponseTraced();
        }

        private IEnumerable<Product> ScanAllProducts()
        {
            AWSXRayRecorder.Instance.AddAnnotation("operationType", "ScanAllProducts");

            List<Product> list;
            var items = LazyTable.Value.Scan(new ScanFilter()).GetRemainingAsync();
            if (items == null)
            {
                throw new ProductNotFoundException("Can't find any product");
            }

            list = new List<Product>(items.Result.Count);
            foreach (var item in items.Result)
            {
                list.Add(BuildProduct(item));
            }
            return list;
        }

        private Product DetailsProduct(string id)
        {
            AWSXRayRecorder.Instance.AddAnnotation("productId", id);
            AWSXRayRecorder.Instance.AddAnnotation("operationType", "DetailsProduct");
            var item = LazyTable.Value.GetItemAsync(id).Result;
            if (item == null)
            {
                throw new ProductNotFoundException("Can't find a product with id = " + id);
            }

            return BuildProduct(item);
        }

        private async Task<Document> CreateProduct<TResult>(Product product)
        {
            AWSXRayRecorder.Instance.AddAnnotation("operationType", "CreateProduct");
            var document = new Document();
            document["Id"] = product.Id;
            document["Name"] = product.Name;
            document["Price"] = product.Price;

            return await LazyTable.Value.PutItemAsync(document);
        }

        private async Task<Document> EditProduct<TResult>(Product product)
        {
            AWSXRayRecorder.Instance.AddAnnotation("productId", product.Id);
            AWSXRayRecorder.Instance.AddAnnotation("operationType", "EditProduct");
            var document = new Document();
            document["Id"] = product.Id;
            document["Name"] = product.Name;
            document["Price"] = product.Price;

            return await LazyTable.Value.UpdateItemAsync(document);
        }

        private async Task<Document> DeleteProduct<TResult>(string id)
        {
            AWSXRayRecorder.Instance.AddAnnotation("productId", id);
            AWSXRayRecorder.Instance.AddMetadata("operationType", "DeleteProduct");
            var document = new Document();
            document["Id"] = id;

            return await LazyTable.Value.DeleteItemAsync(document);
        }

        private Product BuildProduct(Document document)
        {
            var product = new Product();
            product.Id = document["Id"].AsString();
            product.Name = document["Name"].AsString();
            product.Price = document["Price"].AsDecimal();
            return product;
        }

        private void QuerySql(string id)
        {
            var connectionString = ""; // Configure Connection string -> Format : "Data Source=(RDS endpoint),(port number);User ID=(your user name);Password=(your password);"
            using (var sqlConnection = new SqlConnection(connectionString))
            using (var sqlCommand = new TraceableSqlCommand("SELECT " + id, sqlConnection))
            {
                sqlCommand.Connection.Open();
                sqlCommand.ExecuteNonQuery();
            }
        }

        private class ProductNotFoundException : Exception
        {
            public ProductNotFoundException(string message) : base(message) { }
        }
    }
}