using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Security.Cryptography;
using System.Xml.Linq;
using System.Net;
using System.Xml;

namespace WSFatura
{
    public partial class Form1 : Form
    {
        private String[] args = Environment.GetCommandLineArgs();
        private string strEncryptPasswordAT;
        private string strEncryptCreatedAT;
        private string strEncryptNonceAT;
        private string path = AppDomain.CurrentDomain.BaseDirectory.ToString() + "Faturas/";
        public string strDigestAT;
        public Form1()
        {
            InitializeComponent();
            if(args[1] != "")
            {
                string l_file = path + args[1];
                var sFileReader = File.OpenText(l_file);
                string f_linha = sFileReader.ReadToEnd();
                // Carregar o XML
                XDocument doc = XDocument.Parse(f_linha);

                // Obter Username
                XNamespace wss = "http://schemas.xmlsoap.org/ws/2002/12/secext";
                string nif = doc.Descendants(wss + "Username").FirstOrDefault()?.Value;
                Console.WriteLine("Username: " + nif);

                // Obter Password
                string password = doc.Descendants(wss + "Password").FirstOrDefault()?.Value;
                Console.WriteLine("Password: " + password);
                textBox1.Text = nif;
                textBox2.Text = password;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        public void SendFileWithCertificate(string filePath, string Authenticat, string soapAction, string certificatePath, string certificatePassword)
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                // 1. Carregar o certificado com senha
                var cert = new X509Certificate2(certificatePath, certificatePassword);

                // 2. Criar o cliente web para enviar a requisição
                var request = (HttpWebRequest)WebRequest.Create(Authenticat);
                request.Method = "POST";
                request.ContentType = "text/xml; charset=utf-8";
                request.Headers.Add("SOAPAction", soapAction);
                request.ClientCertificates.Add(cert);

                // 3. Criar o envelope SOAP (exemplo básico, precisa adaptar conforme o serviço SOAP)
                string soapEnvelope = filePath;

                // 4. Converter o envelope SOAP em bytes
                byte[] soapBytes = Encoding.UTF8.GetBytes(soapEnvelope);
                request.ContentLength = soapBytes.Length;

                // 5. Enviar os dados
                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(soapBytes, 0, soapBytes.Length);
                }

                // 6. Receber a resposta do servidor
                using (WebResponse response = request.GetResponse())
                {
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string responseText = reader.ReadToEnd();
                        Console.WriteLine("Resposta do Servidor:");
                        //MessageBox.Show(responseText);
                        XDocument doc = XDocument.Parse(responseText);
                        XNamespace wss = "http://factemi.at.min_financas.pt/documents";
                        string Mensagem = doc.Descendants(wss + "Mensagem").FirstOrDefault()?.Value;
                        richTextBox1.Text = Mensagem;
                        Console.WriteLine(responseText);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nif ou Password incorretos!");
                Console.WriteLine("Erro ao enviar o ficheiro via SOAP: " + ex.Message);
            }
        }

        public void EncryptAT(string pPwdAT, string pPathCertf)
        {
            try
            {
                // Load public key from certificate
                var certCP = new System.Security.Cryptography.X509Certificates.X509Certificate2();
                certCP.Import(pPathCertf);
                string publicKeyXml = certCP.PublicKey.Key.ToXmlString(false);

                // Generate symmetric key (Ks)
                var random = new Random();
                var Ks = new byte[16]; // AES 128-bit key
                random.NextBytes(Ks);

                // Encrypt Password (SenhaPF)
                string encryptedPassword = EncryptAES(pPwdAT, Ks);

                // Encrypt Created (DataCriacao)
                string createdDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                string encryptedCreated = EncryptAES(createdDate, Ks);

                // Generate Digest for Password
                string digest = CalculateSHA1Digest(pPwdAT, createdDate, Ks);

                // Encrypt symmetric key with RSA
                string encryptedNonce = EncryptRSA(Ks, publicKeyXml);

                // Assign results
                strEncryptPasswordAT = encryptedPassword;
                strEncryptCreatedAT = encryptedCreated;
                strEncryptNonceAT = encryptedNonce;
                strDigestAT = digest;

                Console.WriteLine("strEncryptPasswordAT: " + strEncryptPasswordAT);
                Console.WriteLine("strEncryptCreatedAT: " + strEncryptCreatedAT);
                Console.WriteLine("strEncryptNonceAT: " + strEncryptNonceAT);
                Console.WriteLine("strDigestAT: " + strDigestAT);
            }
            catch (Exception ex)
            {
                strEncryptPasswordAT = "";
                strEncryptCreatedAT = "";
                strEncryptNonceAT = "";
                strDigestAT = "";
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        private string EncryptAES(string input, byte[] key)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor(aes.Key, null))
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var writer = new StreamWriter(cs))
                    {
                        writer.Write(input);
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        private string CalculateSHA1Digest(string password, string created, byte[] key)
        {
            // Concatenate Password + Created + Key
            string concatenated = password + created + Encoding.UTF8.GetString(key);

            // Calculate SHA-1 Digest
            using (var sha1 = SHA1.Create())
            {
                byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(concatenated));

                // Encrypt digest with AES
                return EncryptAES(Convert.ToBase64String(hash), key);
            }
        }

        private string EncryptRSA(byte[] data, string publicKeyXml)
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(publicKeyXml);
                byte[] encryptedData = rsa.Encrypt(data, false);
                return Convert.ToBase64String(encryptedData);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (checkBox1.Checked == true)
            {
                try
                {
                    //Confirmar
                    if (args.Length >= 2)
                    {
                        string l_file = path + args[1];
                        var sFileReader = File.OpenText(l_file);
                        string f_linha = sFileReader.ReadToEnd();
                        // Carregar o XML
                        XDocument doc = XDocument.Parse(f_linha);

                        // Obter Username
                        XNamespace wss = "http://schemas.xmlsoap.org/ws/2002/12/secext";
                        string username = textBox1.Text;
                        Console.WriteLine("Username: " + username);

                        // Obter Password
                        string password = textBox2.Text;
                        Console.WriteLine("Password: " + password);

                        // Obter InvoiceNo
                        XNamespace ns = "http://factemi.at.min_financas.pt/documents";
                        string invoiceNo = doc.Descendants(ns + "InvoiceNo").FirstOrDefault()?.Value;
                        Console.WriteLine("InvoiceNo: " + invoiceNo);

                        // Obter InvoiceDate
                        string invoiceDate = doc.Descendants(ns + "InvoiceDate").FirstOrDefault()?.Value;
                        Console.WriteLine("InvoiceDate: " + invoiceDate);

                        // Obter TaxPayable
                        string taxPayable = doc.Descendants(ns + "TaxPayable").FirstOrDefault()?.Value;
                        Console.WriteLine("TaxPayable: " + taxPayable);

                        EncryptAT(password, AppDomain.CurrentDomain.BaseDirectory.ToString() + @"AT.cer");

                        // Atualizar NIF
                        var UsernameElement = doc.Descendants(wss + "Username").FirstOrDefault();
                        if (UsernameElement != null)
                        {
                            UsernameElement.Value = username;
                        }

                        // Atualizar Password
                        var passwordElement = doc.Descendants(wss + "Password").FirstOrDefault();
                        if (passwordElement != null)
                        {
                            passwordElement.Value = strEncryptPasswordAT;
                            //passwordElement.SetAttributeValue("Digest", strDigestAT);
                        }

                        // Atualizar Nonce
                        var nonceElement = doc.Descendants(wss + "Nonce").FirstOrDefault();
                        if (nonceElement != null)
                        {
                            nonceElement.Value = strEncryptNonceAT;
                        }

                        // Atualizar Created
                        var createdElement = doc.Descendants(wss + "Created").FirstOrDefault();
                        if (createdElement != null)
                        {
                            createdElement.Value = strEncryptCreatedAT;
                        }

                        string soap = doc.ToString();
                        SendFileWithCertificate(soap, "https://servicos.portaldasfinancas.gov.pt:723/fatcorews/ws/", "http://factemi.at.min_financas.pt/documents", AppDomain.CurrentDomain.BaseDirectory.ToString() + @"TesteWebservices.pfx", "TESTEwebservice");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Erro ao enviar o e-mail: " + ex.Message);
                }

            }
            else
            {
                string l_file = path + args[1];
                var sFileReader = File.OpenText(l_file);
                string f_linha = sFileReader.ReadToEnd();
                // Carregar o XML
                XDocument doc = XDocument.Parse(f_linha);

                // Obter Username
                XNamespace wss = "http://schemas.xmlsoap.org/ws/2002/12/secext";
                string username = textBox1.Text;
                Console.WriteLine("Username: " + username);

                // Obter Password
                string password = textBox2.Text;
                Console.WriteLine("Password: " + password);

                // Obter InvoiceNo
                XNamespace ns = "http://factemi.at.min_financas.pt/documents";
                string invoiceNo = doc.Descendants(ns + "InvoiceNo").FirstOrDefault()?.Value;
                Console.WriteLine("InvoiceNo: " + invoiceNo);

                // Obter InvoiceDate
                string invoiceDate = doc.Descendants(ns + "InvoiceDate").FirstOrDefault()?.Value;
                Console.WriteLine("InvoiceDate: " + invoiceDate);

                // Obter TaxPayable
                string taxPayable = doc.Descendants(ns + "TaxPayable").FirstOrDefault()?.Value;
                Console.WriteLine("TaxPayable: " + taxPayable);

                EncryptAT(password, AppDomain.CurrentDomain.BaseDirectory.ToString() + @"AT.cer");

                // Atualizar NIF
                var UsernameElement = doc.Descendants(wss + "Username").FirstOrDefault();
                if (UsernameElement != null)
                {
                    UsernameElement.Value = username;
                }

                // Atualizar Password
                var passwordElement = doc.Descendants(wss + "Password").FirstOrDefault();
                if (passwordElement != null)
                {
                    passwordElement.Value = strEncryptPasswordAT;
                    //passwordElement.SetAttributeValue("Digest", strDigestAT);
                }

                // Atualizar Nonce
                var nonceElement = doc.Descendants(wss + "Nonce").FirstOrDefault();
                if (nonceElement != null)
                {
                    nonceElement.Value = strEncryptNonceAT;
                }

                // Atualizar Created
                var createdElement = doc.Descendants(wss + "Created").FirstOrDefault();
                if (createdElement != null)
                {
                    createdElement.Value = strEncryptCreatedAT;
                }

                string soap = doc.ToString();
                SendFileWithCertificate(soap, "https://servicos.portaldasfinancas.gov.pt:423/fatcorews/ws/", "http://factemi.at.min_financas.pt/documents", AppDomain.CurrentDomain.BaseDirectory.ToString() + @"506511529.pfx", "novasoft_");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void PB_Logo_Click(object sender, EventArgs e)
        {
            label5.Visible = true;
            checkBox1.Visible = true;
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }
    }
}
