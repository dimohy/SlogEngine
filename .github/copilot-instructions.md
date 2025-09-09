# GitHub Copilot Policy File

## Object Coupling Policy

### Basic Principle
- **Objects must be coupled using interfaces or abstract classes.**
  - This follows the Dependency Inversion Principle.
  - Use interfaces or abstract classes to maintain loose coupling, enhancing code flexibility and testability.

### Exceptions
- **The only exceptions are static classes or sealed classes that do not need further inheritance.**
  - Static classes: Not instantiable, used for utility methods, cannot be inherited.
  - Sealed classes: Do not allow further inheritance, no extension needed.

### Application Examples
- **Correct Coupling**:
  ```csharp
  public interface IService
  {
      void DoSomething();
  }

  public class Service : IService
  {
      public void DoSomething() { /* implementation */ }
  }

  public class Client
  {
      private readonly IService _service;

      public Client(IService service)
      {
          _service = service;
      }

      public void Execute()
      {
          _service.DoSomething();
      }
  }
  ```

- **Incorrect Coupling** (Direct class coupling):
  ```csharp
  public class Service
  {
      public void DoSomething() { /* implementation */ }
  }

  public class Client
  {
      private readonly Service _service; // Direct coupling - avoid

      public Client(Service service)
      {
          _service = service;
      }
  }
  ```

- **Exception Application**:
  ```csharp
  public static class Utility
  {
      public static void Helper() { /* static method */ }
  }

  public sealed class FinalClass
  {
      public void Method() { /* implementation */ }
  }
  ```

### Reasons
- Using interfaces/abstract classes allows easy replacement of implementations, improving maintainability.
- Enables mocking for unit testing.
- Reduces coupling, adhering to SOLID principles.

### Scope of Application
- This policy applies to all code in the SlogEngine project.
- Must be followed when writing new code.
- Refer to this policy when refactoring existing code.

### References
- This policy is based on .NET development best practices.
- For additional questions, consult the team leader or code reviewer.

## Additional Policies

### Naming Conventions
- Use PascalCase for class names, method names, and property names.
- Use camelCase for local variables and parameters.
- Use UPPER_CASE for constants.
- Prefix interfaces with 'I' (e.g., IService).

### Exception Handling
- Use try-catch blocks appropriately and avoid swallowing exceptions.
- Log exceptions with meaningful messages.
- Throw custom exceptions when necessary, inheriting from ApplicationException or appropriate base classes.

### Logging
- Use a logging framework like Serilog or Microsoft.Extensions.Logging.
- Log at appropriate levels: Debug, Information, Warning, Error.
- Include contextual information in logs.

### Unit Testing
- Write unit tests for all public methods.
- Use mocking frameworks like Moq for dependencies.
- Aim for high code coverage.

### Asynchronous Programming
- Use async/await for I/O-bound operations.
- Avoid blocking calls in async methods.
- Use Task for return types in async methods.

### Security
- Validate all inputs to prevent injection attacks.
- Use HTTPS for all communications.
- Store sensitive data securely, using encryption where necessary.

### Documentation and Comments
- Write comments and documentation in Korean, as the target audience is Korean developers.
- Use Korean for user-facing messages, error messages, and documentation.
- Ensure that generated code includes Korean comments to aid understanding for Korean team members.

### Method Documentation Policy
- **All public methods must include XML documentation comments with clear but concise descriptions.**
  - Include `<summary>` tags for method descriptions.
  - Use `<param>` tags for all parameters with their descriptions.
  - Use `<returns>` tags for return value descriptions.
  - Write documentation in Korean for Korean development team.

### Application Examples
- **Correct Documentation**:
  ```csharp
  /// <summary>
  /// 사용자의 블로그 게시물 목록을 조회합니다.
  /// </summary>
  /// <param name="username">조회할 사용자명</param>
  /// <param name="pageSize">한 페이지당 표시할 게시물 수</param>
  /// <returns>블로그 게시물 목록</returns>
  public IReadOnlyList<BlogPost> GetBlogPosts(string username, int pageSize)
  {
      // implementation
  }
  ```

- **Missing Documentation** (Avoid):
  ```csharp
  public IReadOnlyList<BlogPost> GetBlogPosts(string username, int pageSize)
  {
      // implementation - No XML documentation
  }
  ```

### Scope of Application
- This policy applies to all public methods, properties, and classes in the SlogEngine project.
- Must be followed when writing new code.
- Add documentation when refactoring existing code without proper documentation.

### Compile Warnings Policy
- All compile warnings must be resolved with high priority using correct methods.
- Do not suppress warnings; fix them appropriately to maintain code quality.
- Address warnings before committing code to ensure clean builds.

### Response Format
- All responses must start with '({이해도}) 네 주인님.', where {이해도} is the percentage indicating how well the query is understood.
- This ensures consistent and respectful communication.
- Responses should be concise and clear, avoiding verbosity.
- The policy file must be written in English to maintain consistency and accessibility for international contributors.

## Code Review Policy
- All code changes must undergo peer review before merging.
- Use pull requests for code reviews.
- Ensure that reviews cover functionality, performance, security, and adherence to policies.
- Address all review comments before merging.

## Version Control Policy
- Use Git for version control.
- Follow conventional commit messages.
- Create feature branches for new features.
- Merge to main branch only after approval.

## Collection Interface Policy

### Basic Principle
- **Use collection interfaces instead of concrete collection classes to promote loose coupling and testability.**
  - Prefer interfaces such as IList<T>, IReadOnlyList<T>, IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, etc., over concrete types like List<T>, Dictionary<TKey, TValue>, etc.
  - This follows the Dependency Inversion Principle and enhances code flexibility.

### Application Examples
- **Correct Usage**:
  ```csharp
  public interface IBlogService
  {
      IReadOnlyList<BlogPost> GetPosts(string username);
      void AddPost(string username, BlogPost post);
  }
  ```

- **Incorrect Usage** (Direct concrete class usage):
  ```csharp
  public interface IBlogService
  {
      List<BlogPost> GetPosts(string username); // Avoid direct List usage
      void AddPost(string username, BlogPost post);
  }
  ```

### Reasons
- Using interfaces allows for easy replacement of implementations, improving maintainability.
- Enables mocking for unit testing.
- Reduces coupling and adheres to SOLID principles.

### Scope of Application
- This policy applies to all collection types in the SlogEngine project.
- Must be followed when writing new code.
- Refer to this policy when refactoring existing code.

### References
- This policy is based on .NET development best practices.
- For additional questions, consult the team leader or code reviewer.

## Project-Specific Policies

### Basic Principle
- Each project in the SlogEngine solution may have its own `copilot-instructions.md` file.
- When working on a specific project, read and follow the guidelines in its `copilot-instructions.md` file.
- Project-specific policies take precedence over general policies for that project.

### Application Examples
- For SlogEngine.Server, follow general policies plus any server-specific instructions.
- For SlogEngine.WebAssembly, read `SlogEngine.WebAssembly/copilot-instructions.md` and adhere to its guidelines, such as avoiding Blazor-related code.

### Reasons
- Allows for tailored development practices per project.
- Ensures consistency within each project's context.
- Prevents inappropriate code additions based on project type.

### Scope of Application
- Applies to all projects in the SlogEngine solution.
- Must be checked before starting work on a project.
- Refer to the project-specific file for detailed instructions.

### References
- Project-specific files should be located in the root of each project directory.
- For additional questions, consult the team leader or code reviewer.
