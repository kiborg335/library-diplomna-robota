using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibraryApp.Database;
using LibraryApp.Services;
using LibraryApp.Models;

namespace LibraryApp.Forms
{
    public partial class MainForm : Form
    {
        private readonly LibraryDatabase _db;
        private readonly GutenbergImporter _importer;
        private readonly BookDownloader _downloader;

        private TabControl tabControl;
        private TabPage tabGutenberg;
        private TabPage tabMyLibrary;

        private TextBox txtSearch;
        private ComboBox cmbLanguage;
        private ComboBox cmbStatus;
        private DataGridView dgvGutenberg;
        private Button btnSearch;
        private Button btnImport;
        private Button btnReadGutenberg;
        private Button btnDeleteGutenberg;
        private Button btnRefreshGutenberg;
        private Label lblStatus;
        private ProgressBar progressBar;

        private DataGridView dgvMyLibrary;
        private Button btnAddBook;
        private Button btnReadMyLibrary;
        private Button btnDeleteMyLibrary;

        public MainForm()
        {
            InitializeComponent();
            _db = new LibraryDatabase();
            _importer = new GutenbergImporter(_db);
            _downloader = new BookDownloader(_db);

            SetupUI();

            int bookCount = _db.GetBookCount();
            if (bookCount > 0)
            {
                RefreshGutenbergBooks();
            }
            else
            {
                lblStatus.Text = "База порожня. Натисніть «Імпорт каталогу» для завантаження книг.";
            }
            RefreshMyLibrary();
        }

        private void SetupUI()
        {
            this.Text = "Бібліотека книг";
            this.Size = new Size(1200, 750);
            this.StartPosition = FormStartPosition.CenterScreen;

            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;

            tabGutenberg = new TabPage("Каталог Gutenberg");
            tabMyLibrary = new TabPage("Моя бібліотека");

            tabControl.TabPages.Add(tabGutenberg);
            tabControl.TabPages.Add(tabMyLibrary);

            SetupGutenbergTab();
            SetupMyLibraryTab();

            this.Controls.Add(tabControl);
        }

        private void SetupGutenbergTab()
        {
            var searchPanel = new Panel();
            searchPanel.Dock = DockStyle.Top;
            searchPanel.Height = 80;
            searchPanel.Padding = new Padding(10);

            txtSearch = new TextBox();
            txtSearch.Location = new Point(10, 15);
            txtSearch.Width = 300;
            txtSearch.Font = new Font("Segoe UI", 11);
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) RefreshGutenbergBooks(); };

            btnSearch = new Button();
            btnSearch.Text = "Пошук";
            btnSearch.Location = new Point(320, 14);
            btnSearch.Width = 100;
            btnSearch.Height = 28;
            btnSearch.BackColor = Color.LightSteelBlue;
            btnSearch.Click += (s, e) => RefreshGutenbergBooks();

            var lblLang = new Label();
            lblLang.Text = "Мова:";
            lblLang.Location = new Point(440, 18);
            lblLang.Width = 45;

            cmbLanguage = new ComboBox();
            cmbLanguage.Location = new Point(485, 15);
            cmbLanguage.Width = 80;
            cmbLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbLanguage.Items.AddRange(new[] { "Всі", "en", "de", "fr", "uk" });
            cmbLanguage.SelectedIndex = 0;

            var lblStatusF = new Label();
            lblStatusF.Text = "Статус:";
            lblStatusF.Location = new Point(580, 18);
            lblStatusF.Width = 55;

            cmbStatus = new ComboBox();
            cmbStatus.Location = new Point(635, 15);
            cmbStatus.Width = 130;
            cmbStatus.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbStatus.Items.AddRange(new[] { "Всі", "Не завантажено", "Завантажено" });
            cmbStatus.SelectedIndex = 0;

            btnImport = new Button();
            btnImport.Text = "Імпорт каталогу";
            btnImport.Location = new Point(10, 50);
            btnImport.Width = 150;
            btnImport.Height = 28;
            btnImport.BackColor = Color.Orange;
            btnImport.Click += async (s, e) => await ImportCatalogAsync();

            lblStatus = new Label();
            lblStatus.Location = new Point(170, 54);
            lblStatus.Width = 600;
            lblStatus.Font = new Font("Segoe UI", 9);
            lblStatus.ForeColor = Color.DarkBlue;

            searchPanel.Controls.AddRange(new Control[] {
                txtSearch, btnSearch, lblLang, cmbLanguage, lblStatusF, cmbStatus,
                btnImport, lblStatus
            });

            dgvGutenberg = new DataGridView();
            dgvGutenberg.Dock = DockStyle.Fill;
            dgvGutenberg.ReadOnly = true;
            dgvGutenberg.AllowUserToAddRows = false;
            dgvGutenberg.AllowUserToDeleteRows = false;
            dgvGutenberg.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvGutenberg.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvGutenberg.BackgroundColor = Color.White;

            var actionPanel = new Panel();
            actionPanel.Dock = DockStyle.Bottom;
            actionPanel.Height = 50;
            actionPanel.Padding = new Padding(10);

            btnReadGutenberg = new Button();
            btnReadGutenberg.Text = "Читати";
            btnReadGutenberg.Location = new Point(10, 12);
            btnReadGutenberg.Width = 120;
            btnReadGutenberg.Height = 30;
            btnReadGutenberg.BackColor = Color.LightGreen;
            btnReadGutenberg.Click += async (s, e) => await ReadGutenbergBookAsync();

            btnDeleteGutenberg = new Button();
            btnDeleteGutenberg.Text = "Видалити";
            btnDeleteGutenberg.Location = new Point(140, 12);
            btnDeleteGutenberg.Width = 120;
            btnDeleteGutenberg.Height = 30;
            btnDeleteGutenberg.BackColor = Color.LightCoral;
            btnDeleteGutenberg.Click += (s, e) => DeleteGutenbergBook();

            btnRefreshGutenberg = new Button();
            btnRefreshGutenberg.Text = "Оновити";
            btnRefreshGutenberg.Location = new Point(270, 12);
            btnRefreshGutenberg.Width = 120;
            btnRefreshGutenberg.Height = 30;
            btnRefreshGutenberg.BackColor = Color.LightSteelBlue;
            btnRefreshGutenberg.Click += (s, e) => RefreshGutenbergBooks();

            progressBar = new ProgressBar();
            progressBar.Location = new Point(400, 15);
            progressBar.Width = 300;
            progressBar.Height = 23;
            progressBar.Visible = false;

            actionPanel.Controls.AddRange(new Control[] {
                btnReadGutenberg, btnDeleteGutenberg, btnRefreshGutenberg, progressBar
            });

            tabGutenberg.Controls.Add(dgvGutenberg);
            tabGutenberg.Controls.Add(actionPanel);
            tabGutenberg.Controls.Add(searchPanel);
        }

        private void RefreshGutenbergBooks()
        {
            string keyword = txtSearch.Text.Trim();
            string language = cmbLanguage.SelectedIndex > 0 ? cmbLanguage.SelectedItem.ToString() : "";
            string status = "";

            if (cmbStatus.SelectedIndex == 1) status = "catalog";
            else if (cmbStatus.SelectedIndex == 2) status = "downloaded";

            var books = _db.SearchBooks(keyword, language, "", status);

            dgvGutenberg.DataSource = null;
            dgvGutenberg.DataSource = books.Select(b => new
            {
                b.BookId,
                b.Title,
                Автори_та_рік = b.Authors,
                b.Genres,
                b.Language,
                Статус = b.TextStatus == "downloaded" ? "Завантажено" : "Не завантажено",
                b.AddedDate
            }).ToList();

            if (dgvGutenberg.Columns.Contains("BookId"))
                dgvGutenberg.Columns["BookId"].Visible = false;

            lblStatus.Text = "Показано книг: " + dgvGutenberg.RowCount;
        }

        private async Task ImportCatalogAsync()
        {
            btnImport.Enabled = false;
            progressBar.Visible = true;
            progressBar.Style = ProgressBarStyle.Blocks;
            progressBar.Minimum = 0;
            progressBar.Maximum = 100;
            progressBar.Value = 0;
            lblStatus.Text = "Починаю імпорт каталогу...";

            try
            {
                var progress = new Progress<GutenbergProgress>(msg =>
                {
                    this.Invoke(new Action(() =>
                    {
                        lblStatus.Text = msg.Message;
                        progressBar.Value = msg.Percent;
                    }));
                });

                await Task.Run(() => _importer.ImportCatalogAsync(progress));
                RefreshGutenbergBooks();

                MessageBox.Show("Імпорт завершено! Книг у базі: " + _db.GetBookCount(),
                    "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка: " + ex.Message, "Помилка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnImport.Enabled = true;
                progressBar.Visible = false;
            }
        }

        private async Task ReadGutenbergBookAsync()
        {
            if (dgvGutenberg.SelectedRows.Count == 0) return;

            int bookId = (int)dgvGutenberg.SelectedRows[0].Cells["BookId"].Value;
            var book = _db.GetBookById(bookId);
            if (book == null) return;

            string text = _db.GetBookText(bookId);

            if (string.IsNullOrEmpty(text))
            {
                var result = MessageBox.Show(
                    "Текст книги '" + book.Title + "' ще не завантажено. Завантажити зараз?",
                    "Завантаження тексту", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result != DialogResult.Yes) return;

                btnReadGutenberg.Enabled = false;
                progressBar.Visible = true;
                progressBar.Style = ProgressBarStyle.Marquee;
                lblStatus.Text = "Завантажую текст...";

                try
                {
                    text = await _downloader.DownloadBookTextAsync(bookId, book.GutenbergId);
                    RefreshGutenbergBooks();
                    RefreshMyLibrary();
                    lblStatus.Text = "Текст завантажено!";
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Помилка: " + ex.Message, "Помилка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    btnReadGutenberg.Enabled = true;
                    progressBar.Visible = false;
                    return;
                }
                finally
                {
                    btnReadGutenberg.Enabled = true;
                    progressBar.Visible = false;
                }
            }

            if (!string.IsNullOrEmpty(text))
            {
                var readerForm = new ReaderForm(book.Title, text);
                readerForm.ShowDialog();
            }
        }

        private void DeleteGutenbergBook()
        {
            if (dgvGutenberg.SelectedRows.Count == 0) return;

            int bookId = (int)dgvGutenberg.SelectedRows[0].Cells["BookId"].Value;
            string title = dgvGutenberg.SelectedRows[0].Cells["Title"].Value.ToString();

            var result = MessageBox.Show("Видалити книгу '" + title + "'?",
                "Підтвердження", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                _db.DeleteBook(bookId);
                RefreshGutenbergBooks();
                RefreshMyLibrary();
                lblStatus.Text = "Книгу '" + title + "' видалено.";
            }
        }

        private void SetupMyLibraryTab()
        {
            var actionPanel = new Panel();
            actionPanel.Dock = DockStyle.Top;
            actionPanel.Height = 50;
            actionPanel.Padding = new Padding(10);

            btnAddBook = new Button();
            btnAddBook.Text = "Додати книгу";
            btnAddBook.Location = new Point(10, 12);
            btnAddBook.Width = 130;
            btnAddBook.Height = 30;
            btnAddBook.BackColor = Color.LightGreen;
            btnAddBook.Click += (s, e) => AddCustomBook();

            btnReadMyLibrary = new Button();
            btnReadMyLibrary.Text = "Читати";
            btnReadMyLibrary.Location = new Point(150, 12);
            btnReadMyLibrary.Width = 120;
            btnReadMyLibrary.Height = 30;
            btnReadMyLibrary.BackColor = Color.LightBlue;
            btnReadMyLibrary.Click += (s, e) => ReadMyLibraryBook();

            btnDeleteMyLibrary = new Button();
            btnDeleteMyLibrary.Text = "Видалити";
            btnDeleteMyLibrary.Location = new Point(280, 12);
            btnDeleteMyLibrary.Width = 120;
            btnDeleteMyLibrary.Height = 30;
            btnDeleteMyLibrary.BackColor = Color.LightCoral;
            btnDeleteMyLibrary.Click += (s, e) => DeleteMyLibraryBook();

            actionPanel.Controls.AddRange(new Control[] {
                btnAddBook, btnReadMyLibrary, btnDeleteMyLibrary
            });

            dgvMyLibrary = new DataGridView();
            dgvMyLibrary.Dock = DockStyle.Fill;
            dgvMyLibrary.ReadOnly = true;
            dgvMyLibrary.AllowUserToAddRows = false;
            dgvMyLibrary.AllowUserToDeleteRows = false;
            dgvMyLibrary.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvMyLibrary.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvMyLibrary.BackgroundColor = Color.White;

            tabMyLibrary.Controls.Add(dgvMyLibrary);
            tabMyLibrary.Controls.Add(actionPanel);
        }

        private void RefreshMyLibrary()
        {
            var books = _db.GetMyLibraryBooks();

            dgvMyLibrary.DataSource = null;
            dgvMyLibrary.DataSource = books.Select(b => new
            {
                b.BookId,
                b.Title,
                b.Authors,
                b.Genres,
                b.Language,
                Тип = b.TextStatus == "custom_pdf" ? "PDF" :
                      b.TextStatus == "custom_docx" ? "Word" :
                      b.TextStatus == "custom_txt" ? "TXT" :
                      b.TextStatus == "downloaded" ? "Gutenberg" : "Невідомо",
                b.AddedDate
            }).ToList();

            if (dgvMyLibrary.Columns.Contains("BookId"))
                dgvMyLibrary.Columns["BookId"].Visible = false;
        }

        private void AddCustomBook()
        {
            var form = new AddBookFormInternal(_db);
            if (form.ShowDialog() == DialogResult.OK)
            {
                RefreshMyLibrary();
            }
        }

        private void ReadMyLibraryBook()
        {
            if (dgvMyLibrary.SelectedRows.Count == 0) return;

            int bookId = (int)dgvMyLibrary.SelectedRows[0].Cells["BookId"].Value;
            var book = _db.GetBookById(bookId);
            if (book == null) return;

            string text = _db.GetBookText(bookId);

            if (!string.IsNullOrEmpty(text))
            {
                var readerForm = new ReaderForm(book.Title, text);
                readerForm.ShowDialog();
            }
            else if (!string.IsNullOrEmpty(book.TextPath) && File.Exists(book.TextPath))
            {
                try
                {
                    System.Diagnostics.Process.Start(book.TextPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Не вдалося відкрити файл: " + ex.Message,
                        "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Файл цієї книги не знайдено.", "Увага",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void DeleteMyLibraryBook()
        {
            if (dgvMyLibrary.SelectedRows.Count == 0) return;

            int bookId = (int)dgvMyLibrary.SelectedRows[0].Cells["BookId"].Value;
            string title = dgvMyLibrary.SelectedRows[0].Cells["Title"].Value.ToString();

            var result = MessageBox.Show("Видалити книгу '" + title + "'?",
                "Підтвердження", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                _db.DeleteBook(bookId);
                RefreshMyLibrary();
            }
        }
    }

    public class AddBookFormInternal : Form
    {
        private readonly LibraryDatabase _db;
        private string _selectedFilePath = "";

        private TextBox txtTitle;
        private TextBox txtAuthor;
        private TextBox txtYear;
        private TextBox txtLanguage;
        private TextBox txtGenres;
        private TextBox txtDescription;
        private Button btnChooseFile;
        private Label lblFileName;
        private Button btnSave;
        private Button btnCancel;

        public AddBookFormInternal(LibraryDatabase db)
        {
            _db = db;
            SetupUI();
        }

        private void SetupUI()
        {
            this.Text = "Додати книгу";
            this.Size = new Size(500, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            int y = 15;
            int spacing = 35;
            int labelWidth = 100;
            int fieldX = 120;
            int fieldWidth = 340;

            var lblTitle = new Label { Text = "Назва:", Location = new Point(15, y), Width = labelWidth };
            txtTitle = new TextBox { Location = new Point(fieldX, y), Width = fieldWidth };
            y += spacing;

            var lblAuthor = new Label { Text = "Автор:", Location = new Point(15, y), Width = labelWidth };
            txtAuthor = new TextBox { Location = new Point(fieldX, y), Width = fieldWidth };
            y += spacing;

            var lblYear = new Label { Text = "Рік:", Location = new Point(15, y), Width = labelWidth };
            txtYear = new TextBox { Location = new Point(fieldX, y), Width = 80 };
            y += spacing;

            var lblLanguage = new Label { Text = "Мова:", Location = new Point(15, y), Width = labelWidth };
            txtLanguage = new TextBox { Location = new Point(fieldX, y), Width = 80, Text = "uk" };
            y += spacing;

            var lblGenres = new Label { Text = "Жанри:", Location = new Point(15, y), Width = labelWidth };
            txtGenres = new TextBox { Location = new Point(fieldX, y), Width = fieldWidth };
            y += spacing;

            var lblDesc = new Label { Text = "Опис:", Location = new Point(15, y), Width = labelWidth };
            txtDescription = new TextBox { Location = new Point(fieldX, y), Width = fieldWidth, Height = 60, Multiline = true };
            y += 70;

            btnChooseFile = new Button { Text = "Вибрати файл (PDF, Word, TXT)", Location = new Point(15, y), Width = 220, Height = 30 };
            btnChooseFile.Click += (s, e) => ChooseFile();

            lblFileName = new Label { Text = "Файл не вибрано", Location = new Point(245, y + 5), Width = 230, ForeColor = Color.Gray };
            y += 45;

            btnSave = new Button { Text = "Зберегти", Location = new Point(200, y), Width = 120, Height = 35, BackColor = Color.LightGreen };
            btnSave.Click += (s, e) => SaveBook();

            btnCancel = new Button { Text = "Скасувати", Location = new Point(340, y), Width = 120, Height = 35 };
            btnCancel.Click += (s, e) => this.Close();

            this.Controls.AddRange(new Control[] {
                lblTitle, txtTitle, lblAuthor, txtAuthor, lblYear, txtYear,
                lblLanguage, txtLanguage, lblGenres, txtGenres, lblDesc, txtDescription,
                btnChooseFile, lblFileName, btnSave, btnCancel
            });
        }

        private void ChooseFile()
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Filter = "Книги|*.pdf;*.docx;*.doc;*.txt|PDF|*.pdf|Word|*.docx;*.doc|Текст|*.txt|Всі файли|*.*";
                dlg.Title = "Виберіть файл книги";

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    _selectedFilePath = dlg.FileName;
                    lblFileName.Text = Path.GetFileName(_selectedFilePath);
                    lblFileName.ForeColor = Color.Green;
                    if (string.IsNullOrEmpty(txtTitle.Text))
                        txtTitle.Text = Path.GetFileNameWithoutExtension(_selectedFilePath);
                }
            }
        }

        private void SaveBook()
        {
            if (string.IsNullOrEmpty(txtTitle.Text.Trim()))
            {
                MessageBox.Show("Введіть назву книги.", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var book = new Book();
            book.Title = txtTitle.Text.Trim();
            book.Language = txtLanguage.Text.Trim();
            if (string.IsNullOrEmpty(book.Language)) book.Language = "uk";
            book.Downloads = 0;
            book.AuthorIds = new System.Collections.Generic.List<int>();
            book.GenreIds = new System.Collections.Generic.List<int>();

            int year;
            if (int.TryParse(txtYear.Text.Trim(), out year) && year > 0) book.Year = year;

            string authorName = txtAuthor.Text.Trim();
            if (!string.IsNullOrEmpty(authorName))
            {
                int aid = _db.AddOrGetAuthor(authorName);
                if (aid > 0) book.AuthorIds.Add(aid);
            }
            if (book.AuthorIds.Count == 0)
            {
                int uid = _db.AddOrGetAuthor("Невідомий");
                book.AuthorIds.Add(uid);
            }

            if (!string.IsNullOrEmpty(txtGenres.Text.Trim()))
            {
                foreach (var g in txtGenres.Text.Split(','))
                {
                    string genre = g.Trim();
                    if (!string.IsNullOrEmpty(genre))
                    {
                        int gid = _db.AddOrGetGenre(genre);
                        if (gid > 0) book.GenreIds.Add(gid);
                    }
                }
            }

            book.Description = txtDescription.Text.Trim();

            if (!string.IsNullOrEmpty(_selectedFilePath))
            {
                string ext = Path.GetExtension(_selectedFilePath).ToLower();
                string textDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Texts");
                if (!Directory.Exists(textDir)) Directory.CreateDirectory(textDir);

                string destFileName = Guid.NewGuid().ToString() + ext;
                string destPath = Path.Combine(textDir, destFileName);
                File.Copy(_selectedFilePath, destPath);
                book.TextPath = destPath;

                if (ext == ".txt") book.TextStatus = "custom_txt";
                else if (ext == ".pdf") book.TextStatus = "custom_pdf";
                else if (ext == ".docx" || ext == ".doc") book.TextStatus = "custom_docx";
                else book.TextStatus = "custom";
            }
            else
            {
                book.TextStatus = "catalog";
                book.TextPath = "";
            }

            book.GutenbergId = _db.GetNextCustomId();
            _db.AddBook(book);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}