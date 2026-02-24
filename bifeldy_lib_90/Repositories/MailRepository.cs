using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Models;
using bifeldy_lib_90.Services;
using bifeldy_lib_90.TableView;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace bifeldy_lib_90.Repositories {

    public interface IMailRepository {
        MailAddress CreateEmailAddress(string address, string displayName = null, Encoding encoding = null);
        List<MailAddress> CreateEmailAddress(string[] address);
        Attachment CreateEmailAttachment(string filePath);
        List<Attachment> CreateEmailAttachment(string[] filePath);
        MailMessage CreateEmailMessage(string subject, string body, List<MailAddress> to, List<MailAddress> cc = null, List<MailAddress> bcc = null, List<Attachment> attachments = null, MailAddress from = null, Encoding encoding = null);
        Task SendEmailMessage(SmtpClient smtpClient, MailMessage mailMessage);
        Task SendEmailMessage(IDatabase db, MailMessage mailMessage, bool paksaDariHo = false);
        MailAddress GetDefaultBotSenderFromAddress();
        Task CreateAndSend(SmtpClient smtpClient, string subject, string body, List<MailAddress> to, List<MailAddress> cc = null, List<MailAddress> bcc = null, List<Attachment> attachments = null, MailAddress from = null);
        Task CreateAndSend(IDatabase db, string subject, string body, List<MailAddress> to, List<MailAddress> cc = null, List<MailAddress> bcc = null, List<Attachment> attachments = null, MailAddress from = null);
    }

    public sealed class CMailRepository : IMailRepository {

        private readonly EnvVar _envVar;
        private readonly ILogger<CMailRepository> _logger;

        private readonly IApplicationService _as;
        private readonly IGeneralRepository _generalRepo;

        public CMailRepository(
            IOptions<EnvVar> envVar,
            ILogger<CMailRepository> logger,
            IApplicationService @as,
            IGeneralRepository generalRepo
        ) {
            this._envVar = envVar.Value;
            this._logger = logger;
            this._as = @as;
            this._generalRepo = generalRepo;
        }

        private Task<DC_LISTMAILSERVER_T> GetByDcKode(IDatabase db, string dckode) {
            string sqlQuery = "SELECT * FROM dc_listmailserver_t WHERE UPPER(mail_dckode) = :mail_dckode";

            var sqlParam = new DynamicParameters();
            sqlParam.Add("mail_dckode", dckode.ToUpper());

            return db.ExecScalarAsync<DC_LISTMAILSERVER_T>(sqlQuery, sqlParam);
        }

        /* ** */

        private SmtpClient CreateSmtpClient(string host, int port, string uname, string upass) {
            if (string.IsNullOrEmpty(host) || port <= 0 || string.IsNullOrEmpty(uname) || string.IsNullOrEmpty(upass)) {
                throw new Exception("Gagal Mengatur Informasi SMTP!");
            }

            return new SmtpClient() {
                Host = host,
                Port = port,
                Credentials = new NetworkCredential(uname, upass)
            };
        }

        private async Task<SmtpClient> CreateSmtpClient(IDatabase db, bool paksaDariHo = false) {
            string host = null;
            int port = 0;
            string uname = null;
            string upass = null;

            if (paksaDariHo) {
                host = this._envVar.SMTP_SERVER_IP_DOMAIN;
                port = this._envVar.SMTP_SERVER_PORT;
                uname = this._envVar.SMTP_SERVER_USERNAME;
                upass = this._envVar.SMTP_SERVER_PASSWORD;
            }
            else {
                string dcKode = await this._generalRepo.GetKodeDc(db);
                DC_LISTMAILSERVER_T mailServer = await this.GetByDcKode(db, dcKode);

                if (mailServer == null) {
                    return await this.CreateSmtpClient(db, true);
                }

                host = mailServer.MAIL_HOSTNAME;
                string _port = mailServer.MAIL_PORT;
                port = string.IsNullOrEmpty(_port) ? 0 : int.Parse(_port);
                uname = mailServer.MAIL_USERNAME;
                upass = mailServer.MAIL_PASSWORD;
            }

            return this.CreateSmtpClient(host, port, uname, upass);
        }

        public MailAddress CreateEmailAddress(string address, string displayName = null, Encoding encoding = null) {
            return string.IsNullOrEmpty(displayName) ? new MailAddress(address) : new MailAddress(address, displayName, encoding ?? Encoding.UTF8);
        }

        public List<MailAddress> CreateEmailAddress(string[] address) {
            var addresses = new List<MailAddress>();
            foreach (string a in address) {
                addresses.Add(this.CreateEmailAddress(a));
            }

            return addresses;
        }

        public Attachment CreateEmailAttachment(string filePath) => new(filePath);

        public List<Attachment> CreateEmailAttachment(string[] filePath) {
            var attachments = new List<Attachment>();
            foreach (string path in filePath) {
                attachments.Add(this.CreateEmailAttachment(path));
            }

            return attachments;
        }

        public MailAddress GetDefaultBotSenderFromAddress() {
            string version = string.Join("", this._as.AppVersion);
            return this.CreateEmailAddress("sd3@indomaret.co.id", $"[SD3_BOT] 📧 {this._as.AppName} v{version}");
        }

        public MailMessage CreateEmailMessage(
            string subject,
            string body,
            List<MailAddress> to,
            List<MailAddress> cc = null,
            List<MailAddress> bcc = null,
            List<Attachment> attachments = null,
            MailAddress from = null,
            Encoding encoding = null
        ) {
            encoding ??= Encoding.UTF8;

            var mailMessage = new MailMessage() {
                Subject = subject,
                SubjectEncoding = encoding,
                Body = body,
                BodyEncoding = encoding,
                From = from ?? this.GetDefaultBotSenderFromAddress(),
                IsBodyHtml = true
            };

            foreach (MailAddress t in to) {
                mailMessage.To.Add(t);
            }

            if (cc != null) {
                foreach (MailAddress c in cc) {
                    mailMessage.CC.Add(c);
                }
            }

            if (bcc != null) {
                foreach (MailAddress b in bcc) {
                    mailMessage.Bcc.Add(b);
                }
            }

            if (attachments != null) {
                foreach (Attachment a in attachments) {
                    mailMessage.Attachments.Add(a);
                }
            }

            return mailMessage;
        }

        public Task SendEmailMessage(SmtpClient smtpClient, MailMessage mailMessage) {
            return smtpClient.SendMailAsync(mailMessage);
        }

        public async Task SendEmailMessage(IDatabase db, MailMessage mailMessage, bool paksaDariHo = false) {
            SmtpClient smtpClient = await this.CreateSmtpClient(db, paksaDariHo);
            await this.SendEmailMessage(smtpClient, mailMessage);
        }

        public async Task CreateAndSend(
            SmtpClient smtpClient,
            string subject,
            string body,
            List<MailAddress> to,
            List<MailAddress> cc = null,
            List<MailAddress> bcc = null,
            List<Attachment> attachments = null,
            MailAddress from = null
        ) {
            try {
                MailMessage mail = this.CreateEmailMessage(
                    subject, body, to, cc, bcc, attachments,
                    from ?? this.GetDefaultBotSenderFromAddress()
                );

                await this.SendEmailMessage(smtpClient, mail);
            }
            catch (Exception ex) {
                this._logger.LogError("[SUREL_CREATE_AND_SEND] {ex}", ex.Message);
                throw;
            }
        }

        public async Task CreateAndSend(
            IDatabase db,
            string subject,
            string body,
            List<MailAddress> to,
            List<MailAddress> cc = null,
            List<MailAddress> bcc = null,
            List<Attachment> attachments = null,
            MailAddress from = null
        ) {
            try {
                MailMessage mail = this.CreateEmailMessage(
                    subject, body, to, cc, bcc, attachments,
                    from ?? this.GetDefaultBotSenderFromAddress()
                );

                try {
                    // Pakai Regional
                    await this.SendEmailMessage(db, mail);
                }
                catch {
                    // Via DCHO
                    await this.SendEmailMessage(db, mail, true);
                }
            }
            catch (Exception ex) {
                this._logger.LogError("[SUREL_CREATE_AND_SEND] {ex}", ex.Message);
                throw;
            }
        }

    }

}