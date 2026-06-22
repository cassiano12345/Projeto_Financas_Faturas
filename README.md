### 🧾Projeto envio de faturas

 O presente projeto é destinado ao envio de faturas, documentos de conferência e recibos a Autoridade Tributária e Aduaneira (AT), por meio de uma API fornecida pela mesma, foi também fornecida uma chave publica e um certificado ssl, que serão usado para criptografar a password, a data e o nonce, feito isso os dados são organizados num ficheiro SOAP e so assim são enviados via API. Para mais informações ver "Manual de integração de software" em links.

 ### Algumas funcionalidades a destacar
Ficheiro->Projeto_Financas_Faturas_C#->Form1.cs

***Funções***
- EncryptAT: A função tem como variáveis de entrada duas variáveis uma para a password, e o caminho da chave publica, o principal objetivo da função é gerar a chave simetrica "AES", e com a mesma chave simetrica chamar outras funções para criptografar a password, a data, e o nonce.

- EncryptAES: Esta função recebe uma string input, e uma chave simetrica AES, o principal objetivo da função é criptografar valores, para tal primeiro é criado o modo de criptofrafia onde foi usado ECB, e PKC7, depois é iniciado o processo de criptografia, onde é convertida a string para bytes, onde é criptografado os bytes, e no final são convertidos para base 64. Foi a função usada para criptografar a password e a data.

- EncryptRSA: Esta função recebe a chave simetrica, e a chave RSA que esta na chave publica. O objetivo da função é criar uma chave simetrica para o Nonce com a RSA, começando por criptografalos e no final retornar os dados convertidos em base 64. 

- SendFileWithCertificate: A função tem como objetivo enviar os dados da fatura, para tal a função recebe o ficheiro SOAP ja com a password, data, e nonce criptografados, o link da API para fazer a autenticação, o link da ação SOAP na API, o caminho do certificado, e a password do certificado. O primeiro passo foi carregar o certificado com a senha, depois foi criado o header onde foi definido o metodo "POST", depois foi criado o envelope com os dados SOAP, foi convertido o envelope em bytes, o passo seguinte foi enviar os dados, e no final é recebida a resposta onde é possivel ver se esta tudo OK ou se a declaração tem algum erro de contablidade. 

***Variáveis***
- StrEncryptPasswordAT: Variavel destinada a guardar a password criptografada. <br/>

- StrEncryptCreatedAT: Variavel destinada a guardar a data criptografada. <br/>

- StrEncryptNonceAT: Variavel destinada a guardar o Nonce criptografado.<br/>

***Imagens*** <br/>

***Página principal*** <br/>
A imagem a baixo mostra a página principal do programa, onde é possivel ver os campos do NIF, da password, e o campo destinado a mostrar a resposta do servidor. 

<p align="center">
  <img src="Imagem principal do programa.png" alt="OpenMontage" width="700">
</p>


***Links***
- Link da API de testes destinado a autenticação <br/>
https://servicos.portaldasfinancas.gov.pt:723/fatcorews/ws/

- Link da API de produção destinado a autenticação <br/>
https://servicos.portaldasfinancas.gov.pt:423/fatcorews/ws/
  
- Link da ação SOAP na API <br/>
http://factemi.at.min_financas.pt/documents

- Site do E fatura <br/>
https://faturas.portaldasfinancas.gov.pt/

- Site das Finanças <br/>
https://www.portaldasfinancas.gov.pt/at/html/index.html

- Manual de integração de software <br/>
https://info.portaldasfinancas.gov.pt/pt/apoio_ao_contribuinte/Outras_entidades/Suporte_tecnologico/Webservice/e_Fatura/Documents/Comunicacao_dos_elementos_dos_documentos_de_faturacao_aspetos_gerais.pdf
