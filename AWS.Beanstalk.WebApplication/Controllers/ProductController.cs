using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Util;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Handlers.SqlServer;
using Amazon.XRay.Recorder.Handlers.System.Net;
using AWS.Beanstalk.WebApplication.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace AWS.Beanstalk.WebApplication.Controllers
{
    public class ProductController : Controller
    {
        static readonly Lazy<AmazonDynamoDBClient> LazyDdbClient;

        static readonly Lazy<Table> LazyTable;

        static ProductController()
        {
            LazyDdbClient = new Lazy<AmazonDynamoDBClient>(() =>
            {
                RegionEndpoint r;
                try
                {
                    r = EC2InstanceMetadata.Region;
                }
                catch (Exception)
                {
                    r = RegionEndpoint.EUWest3; // configure here the default region, when running locally 
                }

                var client = new AmazonDynamoDBClient(r);

                return client;
            });

            LazyTable = new Lazy<Table>(() =>
            {
                var tableName = "Product";
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
                AWSXRayRecorder.Instance.AddAnnotation("Get", id);

                var product = AWSXRayRecorder.Instance.TraceMethod<Product>("DetailsProduct", () => DetailsProduct(id));

                // Trace out-going HTTP request
                AWSXRayRecorder.Instance.TraceMethod("Outgoing Http Web Request", () => MakeDummyHttpWebRequest(id));

                CustomSubsegment("we are inside method: DetailsProduct", 500);

                // Trace SQL query
                //  AWSXRayRecorder.Instance.TraceMethod("Query SQL", () => QuerySql(id));

                return View(product);
            }
            catch (ProductNotFoundException e)
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
            AWSXRayRecorder.Instance.AddAnnotation("WebRequestCall", id);
            HttpWebRequest request = null;
            request = (HttpWebRequest)WebRequest.Create("http://www.amazon.com");
            request.GetResponseTraced();
        }

        private IEnumerable<Product> ScanAllProducts()
        {
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
            var item = LazyTable.Value.GetItemAsync(id).Result;
            if (item == null)
            {
                throw new ProductNotFoundException("Can't find a product with id = " + id);
            }

            return BuildProduct(item);
        }

        private async Task<Document> CreateProduct<TResult>(Product product)
        {
            var document = new Document();
            document["Id"] = product.Id;
            document["Name"] = product.Name;
            document["Price"] = product.Price;

            return await LazyTable.Value.PutItemAsync(document);
        }

        private async Task<Document> EditProduct<TResult>(Product product)
        {
            var document = new Document();
            document["Id"] = product.Id;
            document["Name"] = product.Name;
            document["Price"] = product.Price;

            return await LazyTable.Value.UpdateItemAsync(document);
        }

        private async Task<Document> DeleteProduct<TResult>(string id)
        {
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