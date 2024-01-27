using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Newtonsoft.Json;

namespace FunctionApp1
{
    public static class Function1
    {
        public class ProdutoPedido
        {
            public string NomeProduto { get; set; }
            public int Estoque { get; set; }
            public double? Valor { get; set; }

            public ProdutoPedido(string nomeProduto, int estoque, double valor ) {

                this.NomeProduto = nomeProduto;
                this.Estoque = estoque;
                this.Valor = valor;
            
            }
        }

        public class Pedido
        {
            public int IdPedido { get; set; }
            public List<ProdutoPedido> Produtos { get; set; } = new List<ProdutoPedido>();
            public string CartaoCliente { get; set; }
            public double? ValorTotal { get; set; }

          
            public Pedido()
            {
                Random random = new Random();
                this.IdPedido = random.Next(100, 1000);
            }
        }

        public class Cliente
        {
            public string Nome { get; set; }
            public string NumeroCartao { get; set; }
            public double Saldo { get; set; }
            public Cliente() { }

            public Cliente(string nome, string numeroCartao, double saldo)
            {
                Nome = nome;
                NumeroCartao = numeroCartao;
                Saldo = saldo;
            }
        }

        [FunctionName("Function1")]
        public static async Task<string>RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
      
            try
            {
                var pedido = context.GetInput<Pedido>();
          
                var valor = await context.CallActivityAsync<double>(nameof(VerificacaoProduto), pedido);

                if (valor > 0)
                {
                    pedido.ValorTotal = valor;
                    
                    var retorno = await context.CallActivityAsync<bool>(nameof(VerificacaoCliente), pedido);

                    if (retorno)
                    {
                        var prazoEntrega = await context.CallActivityAsync<DateTime>(nameof(VerificaPrazoEntrega), pedido);
                        return $"Compra aprovada, o cliente possui saldo e os produtos estão disponiveis!! Prazo de entrega: {prazoEntrega.ToString("dd/MM/yyyy")}";
                    }
                    else
                        return $"Compra falhou, o cliente não existe e/ou não possui saldo suficiente!";
                }
                else
                {
                    return $"Alguns produtos não estão disponiveis";
                }
            }
            catch (System.Exception e)
            {

                return (e.Message);
            }
            

        }

        [FunctionName(nameof(VerificacaoProduto))]
        public static double? VerificacaoProduto([ActivityTrigger] Pedido pedido, ILogger log)
        {
            log.LogInformation($"Verificando produtos de pedido com ID {pedido.IdPedido}...");
            
            //Lista de produtos
            var produtos = new List<ProdutoPedido>();

            produtos.Add(new ProdutoPedido("TV Samsung 40", 25, 1500));
            produtos.Add(new ProdutoPedido("Geladeira Brastemp 400l", 5, 3000));
            produtos.Add(new ProdutoPedido("Iphone 15 128gb", 50, 5000));

            double? valorTotal = 0;
            int qtdProdEncontrado = 0;
            List<string> ProdutosNEncontrados = new List<string>();

            foreach (var produto in pedido.Produtos)
            {
                var produtoEncontrado = produtos.FirstOrDefault(p => p.NomeProduto == produto.NomeProduto && p.Estoque >= produto.Estoque);

                if (produtoEncontrado != null)
                {
                    valorTotal += produtoEncontrado.Valor * produto.Estoque;
                   
                    qtdProdEncontrado++;
                }
                else
                    ProdutosNEncontrados.Add(produto.NomeProduto);
            }

            if (pedido.Produtos.Count != qtdProdEncontrado)
                return 0;
            else
                return valorTotal;
           

        }

        [FunctionName(nameof(VerificacaoCliente))]
        public static bool VerificacaoCliente([ActivityTrigger] Pedido pedido, ILogger log)
        {
            log.LogInformation($"Verificando se cliente com cartão de numero: {pedido.CartaoCliente} possui saldo");

            //Lista de clientes
            List<Cliente> clientes = new List<Cliente>();

            clientes.Add(new Cliente("Bruno", "555588889999", 10000));
            clientes.Add(new Cliente("Tiago", "666688883333", 3000));
            clientes.Add(new Cliente("Victor", "111155552222", 5000));
            clientes.Add(new Cliente("Marcos", "777733334444", 7000));

            var saldo = clientes.Where(c=> c.NumeroCartao == pedido.CartaoCliente).Select(c=> c.Saldo).FirstOrDefault();

            if(saldo > pedido.ValorTotal)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        [FunctionName(nameof(VerificaPrazoEntrega))]
        public static DateTime VerificaPrazoEntrega([ActivityTrigger] Pedido pedido, ILogger log)
        {

            log.LogInformation($"Verificando prazo de entrega");

            DateTime data = DateTime.Now;

            foreach (var produto in pedido.Produtos)
            {
               data = data.AddDays(produto.Estoque + 2);
            }
            
            return data;
        }

        [FunctionName("Function1_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous,  "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            var content = await  req.Content.ReadAsStringAsync();
            dynamic pedido = JsonConvert.DeserializeObject<Pedido>(content);
      
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("Function1", pedido );

            log.LogInformation("Orchestração iniciada com ID = '{instanceId}'.", instanceId);

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}