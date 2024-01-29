# FunctionAzureTech

FIAP - Grupo XX - TECH CHALLENGE

- Bruno Moura     - RM 350846
- Marcos André    - RM 351923
- Tiago Vazzoller - RM 351733
- Victor Hugo     - RM 351315


Repositório com fonte de durable function que simula aprovação de pedidos, a mesma está publicada no azure e pode ser testada através de chamada POST abaixo:

https://functionappoctech.azurewebsites.net/api/Function1_HttpStart?

Exemplo de chamada:


{
#    "Produtos": 
    [
        {"NomeProduto": "Geladeira Brastemp 400l","Estoque": 2},
        {"NomeProduto": "TV Samsung 40","Estoque": 1}
    ],
    "CartaoCliente" : "555588889999"

# }


- A mesma possui 3 Actions e pode ter 3 retornos diferentes com base no input:
   - Compra aprovada, o cliente possui saldo e os produtos estão disponiveis!! Prazo de entrega dd//MM/yyyy
   - Alguns produtos não estão disponiveis
   - Compra falhou, o cliente não existe e/ou não possui saldo suficiente!
