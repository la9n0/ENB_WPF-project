namespace ENB_project.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _db;

        public IUserRepository        Users        { get; }
        public INodeRepository        Nodes        { get; }
        public INoteContentRepository NoteContents { get; }
        public ICategoryRepository    Categories   { get; }
        public IReminderRepository    Reminders    { get; }

        public UnitOfWork()
        {
            _db          = new AppDbContext();
            Users        = new UserRepository(_db);
            Nodes        = new NodeRepository(_db);
            NoteContents = new NoteContentRepository(_db);
            Categories   = new CategoryRepository(_db);
            Reminders    = new ReminderRepository(_db);
        }

        public void Commit()   => _db.SaveChanges();
        public void Rollback() => _db.ChangeTracker.Clear();

        public async Task CommitAsync()   => await _db.SaveChangesAsync();
        public async Task RollbackAsync() => await Task.Run(() => _db.ChangeTracker.Clear());

        public void Dispose() => _db.Dispose();
    }
}