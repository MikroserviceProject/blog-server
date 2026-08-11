import os
import re

services_dir = '/Users/salihacicek/Desktop/tapukadastro/blog-server/Authentication.CORE/Services'
files = ['AuthService.cs', 'UserProfileService.cs', 'AdminService.cs']

for f in files:
    path = os.path.join(services_dir, f)
    with open(path, 'r') as file:
        content = file.read()
    
    # 1. Add AutoMapper Usings
    if 'using AutoMapper;' not in content:
        content = content.replace('using AuthenticationService.Core.Interfaces;', 'using AuthenticationService.Core.Interfaces;\nusing AutoMapper;')

    # 2. Inject IMapper to Constructor
    if 'IMapper _mapper;' not in content:
        content = content.replace('private readonly IUnitOfWork _unitOfWork;', 'private readonly IUnitOfWork _unitOfWork;\n    private readonly IMapper _mapper;')
        # AuthService
        if 'public AuthService(' in content:
            content = content.replace('public AuthService(\n        IUnitOfWork unitOfWork,', 'public AuthService(\n        IUnitOfWork unitOfWork,\n        IMapper mapper,')
            content = content.replace('_unitOfWork = unitOfWork;', '_unitOfWork = unitOfWork;\n        _mapper = mapper;')
        # UserProfileService
        elif 'public UserProfileService(' in content:
            content = content.replace('public UserProfileService(\n        IUnitOfWork unitOfWork,', 'public UserProfileService(\n        IUnitOfWork unitOfWork,\n        IMapper mapper,')
            content = content.replace('_unitOfWork = unitOfWork;', '_unitOfWork = unitOfWork;\n        _mapper = mapper;')
        # AdminService
        elif 'public AdminService(' in content:
            content = content.replace('public AdminService(\n        IUnitOfWork unitOfWork,', 'public AdminService(\n        IUnitOfWork unitOfWork,\n        IMapper mapper,')
            content = content.replace('_unitOfWork = unitOfWork;', '_unitOfWork = unitOfWork;\n        _mapper = mapper;')
            
    # 3. EF ILike Optimization
    content = content.replace('u.Username.ToLower().Contains(term)', 'EF.Functions.ILike(u.Username, $"%{searchTerm}%")')
    content = content.replace('u.Email.ToLower().Contains(term)', 'EF.Functions.ILike(u.Email, $"%{searchTerm}%")')

    # 4. MapToUserDto Replacements
    content = content.replace('MapToUserDto(user)', '_mapper.Map<UserDto>(user)')
    content = content.replace('MapToUserDto(adminUser)', '_mapper.Map<UserDto>(adminUser)')
    content = content.replace('users.Select(MapToUserDto)', 'users.Select(u => _mapper.Map<UserDto>(u))')
    
    with open(path, 'w') as file:
        file.write(content)

print("AutoMapper refactored.")
