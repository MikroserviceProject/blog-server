import os

services_dir = '/Users/salihacicek/Desktop/tapukadastro/blog-server/Authentication.CORE/Services'
files = ['AuthService.cs', 'UserProfileService.cs', 'AdminService.cs']

for f in files:
    path = os.path.join(services_dir, f)
    with open(path, 'r') as file:
        content = file.read()
    
    if 'using AuthenticationService.Core.Interfaces.Repositories;' not in content:
        content = content.replace('using AuthenticationService.Core.Interfaces;', 'using AuthenticationService.Core.Interfaces;\nusing AuthenticationService.Core.Interfaces.Repositories;')
    
    content = content.replace('private readonly AppDbContext _context;', 'private readonly IUnitOfWork _unitOfWork;')
    content = content.replace('AppDbContext context,', 'IUnitOfWork unitOfWork,')
    content = content.replace('_context = context;', '_unitOfWork = unitOfWork;')
    
    # Specific edge cases to map to standard GenericRepository methods
    content = content.replace('_context.Users.Add(', '_unitOfWork.Repository<User>().AddAsync(')
    content = content.replace('_context.UserNotifications.Add(', '_unitOfWork.Repository<UserNotification>().AddAsync(')
    content = content.replace('_context.Users.AsQueryable()', '_unitOfWork.Repository<User>().Query()')
    content = content.replace('_context.UserNotifications.AsQueryable()', '_unitOfWork.Repository<UserNotification>().Query()')
    content = content.replace('_context.Users.FindAsync(', '_unitOfWork.Repository<User>().GetByIdAsync(')
    content = content.replace('_context.UserNotifications.FindAsync(', '_unitOfWork.Repository<UserNotification>().GetByIdAsync(')
    content = content.replace('_context.Users.', '_unitOfWork.Repository<User>().Query().')
    content = content.replace('_context.UserNotifications.', '_unitOfWork.Repository<UserNotification>().Query().')
    
    # Clean up double query if any
    content = content.replace('.Query().Query()', '.Query()')
    content = content.replace('.Query().AddAsync(', '.AddAsync(')
    content = content.replace('.Query().GetByIdAsync(', '.GetByIdAsync(')
    content = content.replace('_context.SaveChangesAsync()', '_unitOfWork.SaveChangesAsync()')
    
    with open(path, 'w') as file:
        file.write(content)

print("Refactored services successfully.")
