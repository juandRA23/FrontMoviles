﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using FrontMoviles.Modelos;

// Servicios/ApiService.cs
using System.Text.Json.Serialization;

namespace FrontMoviles.Servicios
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private const string BASE_URL = "http://localhost:56387/"; // Cambia por tu URL base //""; "http://10.0.2.2:56387/"


        public ApiService()
        {
            // ✅ USAR EL HANDLER PERSONALIZADO PARA JWT
            var jwtHandler = new JwtHttpHandler();

            _httpClient = new HttpClient(jwtHandler);
            _httpClient.BaseAddress = new Uri(BASE_URL);
            _httpClient.Timeout = TimeSpan.FromSeconds(30); // Timeout de 30 segundos

            // Headers por defecto
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "ServiFlex-Mobile/1.0");

            System.Diagnostics.Debug.WriteLine("✅ ApiService creado con JwtHttpHandler");
        }
        #region Métodos de Autenticación por Sesión

        // Método para configurar headers de autenticación
        private void ConfigurarAutenticacion()
        {
            var token = SessionManager.ObtenerToken();
            var sessionId = SessionManager.ObtenerSessionId();

            // Limpiar headers anteriores
            _httpClient.DefaultRequestHeaders.Remove("Authorization");
            _httpClient.DefaultRequestHeaders.Remove("SessionId");

            // Agregar headers de autenticación
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            }

            if (!string.IsNullOrEmpty(sessionId))
            {
                _httpClient.DefaultRequestHeaders.Add("SessionId", sessionId);
            }
        }

        #endregion

        #region Métodos de Usuario

        // Método para registrar usuario
        public async Task<ResInsertarUsuario> RegistrarUsuarioAsync(ReqInsertarUsuario request)
        {
            try
            {
                // Serializar el objeto a JSON
                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Hacer la petición POST
                var response = await _httpClient.PostAsync("api/usuario/insertar", content);

                // Leer la respuesta
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // Deserializar la respuesta exitosa
                    var result = JsonSerializer.Deserialize<ResInsertarUsuario>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return result ?? CreateInsertarUsuarioErrorResponse(-4, "Respuesta vacía del servidor");
                }
                else
                {
                    // Si hay error HTTP, intentar deserializar el error
                    try
                    {
                        var errorResult = JsonSerializer.Deserialize<ResInsertarUsuario>(responseContent, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        return errorResult ?? CreateInsertarUsuarioErrorResponse((int)response.StatusCode, $"Error HTTP: {response.StatusCode}");
                    }
                    catch
                    {
                        return CreateInsertarUsuarioErrorResponse((int)response.StatusCode, responseContent);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                return CreateInsertarUsuarioErrorResponse(-1, $"Error de conexión: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                return CreateInsertarUsuarioErrorResponse(-2, "Tiempo de espera agotado");
            }
            catch (Exception ex)
            {
                return CreateInsertarUsuarioErrorResponse(-3, $"Error inesperado: {ex.Message}");
            }
        }

        private ResInsertarUsuario CreateInsertarUsuarioErrorResponse(int errorCode, string message)
        {
            var errorList = new List<ErrorItem>();
            var errorItem = new ErrorItem
            {
                ErrorCode = errorCode,
                Message = message
            };
            errorList.Add(errorItem);

            return new ResInsertarUsuario
            {
                Resultado = false,
                Error = errorList
            };
        }

        // Método mejorado para obtener usuario usando sesión
        public async Task<ResObtenerUsuario> ObtenerUsuarioConSesionAsync()
        {
            try
            {
                // Verificar si hay sesión activa
                if (!SessionManager.EstaLogueado())
                {
                    return CreateObtenerUsuarioErrorResponse(-10, "No hay sesión activa. Por favor, inicia sesión.");
                }

                // Configurar autenticación
                ConfigurarAutenticacion();

                // Obtener información de sesión
                var userEmail = SessionManager.ObtenerEmailUsuario();

                if (string.IsNullOrEmpty(userEmail))
                {
                    return CreateObtenerUsuarioErrorResponse(-11, "No se encontró información de usuario en la sesión.");
                }

                // Crear el request con la información de sesión
                var request = new ReqObtenerUsuario
                {
                    Usuario = new UsuarioObtener
                    {
                        UsuarioId = 0, // Usar 0 cuando se busca por email
                        Correo = userEmail
                    }
                };

                // Serializar el objeto a JSON
                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                // Log para debugging
                System.Diagnostics.Debug.WriteLine($"Request JSON: {json}");
                System.Diagnostics.Debug.WriteLine($"Token: {SessionManager.ObtenerToken()}");
                System.Diagnostics.Debug.WriteLine($"SessionId: {SessionManager.ObtenerSessionId()}");
                System.Diagnostics.Debug.WriteLine($"UserEmail: {userEmail}");

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Hacer la petición POST
                var response = await _httpClient.PostAsync("api/usuario/obtener", content);

                // Leer la respuesta
                var responseContent = await response.Content.ReadAsStringAsync();

                // Log para debugging
                System.Diagnostics.Debug.WriteLine($"Response Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Response Content: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        };

                        var result = JsonSerializer.Deserialize<ResObtenerUsuario>(responseContent, options);

                        if (result == null)
                        {
                            return CreateObtenerUsuarioErrorResponse(-4, "Respuesta vacía del servidor");
                        }

                        return result;
                    }
                    catch (JsonException jsonEx)
                    {
                        return CreateObtenerUsuarioErrorResponse(-5, $"Error al procesar respuesta del servidor: {jsonEx.Message}");
                    }
                }
                else
                {
                    // Si es error 401 (No autorizado), la sesión puede haber expirado
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        SessionManager.CerrarSesion();
                        return CreateObtenerUsuarioErrorResponse(-12, "Sesión inválida. Por favor, inicia sesión nuevamente.");
                    }

                    // Intentar deserializar el error
                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        };

                        var errorResult = JsonSerializer.Deserialize<ResObtenerUsuario>(responseContent, options);
                        if (errorResult != null)
                        {
                            return errorResult;
                        }
                    }
                    catch
                    {
                        // Si no se puede deserializar, crear error genérico
                    }

                    return CreateObtenerUsuarioErrorResponse((int)response.StatusCode, $"Error del servidor ({response.StatusCode}): {responseContent}");
                }
            }
            catch (HttpRequestException ex)
            {
                return CreateObtenerUsuarioErrorResponse(-1, $"Error de conexión: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                return CreateObtenerUsuarioErrorResponse(-2, "Tiempo de espera agotado");
            }
            catch (Exception ex)
            {
                return CreateObtenerUsuarioErrorResponse(-3, $"Error inesperado: {ex.Message}");
            }
        }

        // Método original para obtener usuario (mantener compatibilidad)
        public async Task<ResObtenerUsuario> ObtenerUsuarioAsync(string correo = null, int usuarioId = 0)
        {
            try
            {
                // Crear el request con la información disponible
                var request = new ReqObtenerUsuario
                {
                    Usuario = new UsuarioObtener
                    {
                        UsuarioId = usuarioId,
                        Correo = correo ?? string.Empty,
                        // Inicializar otros campos para evitar errores de serialización
                        Nombre = "",
                        Apellido1 = "",
                        Apellido2 = "",
                        FechaNacimiento = default(DateTime),
                        FotoPerfil = "",
                        Telefono = "",
                        Direccion = "",
                        Contrasena = "",
                        Salt = "",
                        Verificacion = 0,
                        Activo = true,
                        PerfilCompleto = false,
                        CreatedAt = default(DateTime),
                        UpdatedAt = default(DateTime)
                    }
                };

                // Serializar el objeto a JSON
                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Hacer la petición POST
                var response = await _httpClient.PostAsync("api/usuario/obtener", content);

                // Leer la respuesta
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        };

                        var result = JsonSerializer.Deserialize<ResObtenerUsuario>(responseContent, options);

                        if (result == null)
                        {
                            return CreateObtenerUsuarioErrorResponse(-4, "Respuesta vacía del servidor");
                        }

                        return result;
                    }
                    catch (JsonException jsonEx)
                    {
                        return CreateObtenerUsuarioErrorResponse(-5, $"Error al procesar respuesta del servidor: {jsonEx.Message}");
                    }
                }
                else
                {
                    return CreateObtenerUsuarioErrorResponse((int)response.StatusCode, $"Error del servidor: {responseContent}");
                }
            }
            catch (HttpRequestException ex)
            {
                return CreateObtenerUsuarioErrorResponse(-1, $"Error de conexión: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                return CreateObtenerUsuarioErrorResponse(-2, "Tiempo de espera agotado");
            }
            catch (Exception ex)
            {
                return CreateObtenerUsuarioErrorResponse(-3, $"Error inesperado: {ex.Message}");
            }
        }

        private ResObtenerUsuario CreateObtenerUsuarioErrorResponse(int errorCode, string message)
        {
            var errorList = new List<ErrorItem>();
            var errorItem = new ErrorItem
            {
                ErrorCode = errorCode,
                Message = message
            };
            errorList.Add(errorItem);

            return new ResObtenerUsuario
            {
                Resultado = false,
                Error = errorList,
                Usuario = null
            };
        }

        #endregion

        #region Métodos de Login

        public async Task<ResLoginUsuario> LoginUsuarioAsync(ReqLoginUsuario request)
        {
            try
            {
                // Serializar el objeto a JSON
                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Hacer la petición POST
                var response = await _httpClient.PostAsync("api/usuario/login", content);

                // Leer la respuesta
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        // Opciones de deserialización más flexibles
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        };

                        var result = JsonSerializer.Deserialize<ResLoginUsuario>(responseContent, options);

                        return result ?? CreateLoginErrorResponse(-4, "Respuesta vacía del servidor");
                    }
                    catch (JsonException jsonEx)
                    {
                        // Error de deserialización - intentar manualmente
                        return CreateLoginErrorResponse(-5, $"Error al procesar respuesta del servidor: {jsonEx.Message}");
                    }
                }
                else
                {
                    // Si hay error HTTP, intentar deserializar el error
                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        };

                        var errorResult = JsonSerializer.Deserialize<ResLoginUsuario>(responseContent, options);
                        return errorResult ?? CreateLoginErrorResponse((int)response.StatusCode, $"Error HTTP: {response.StatusCode}");
                    }
                    catch
                    {
                        return CreateLoginErrorResponse((int)response.StatusCode, $"Error del servidor: {responseContent}");
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                return CreateLoginErrorResponse(-1, $"Error de conexión: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                return CreateLoginErrorResponse(-2, "Tiempo de espera agotado");
            }
            catch (Exception ex)
            {
                return CreateLoginErrorResponse(-3, $"Error inesperado: {ex.Message}");
            }
        }

        private ResLoginUsuario CreateLoginErrorResponse(int errorCode, string message)
        {
            var errorList = new List<ErrorItem>();
            var errorItem = new ErrorItem
            {
                ErrorCode = errorCode,
                Message = message
            };
            errorList.Add(errorItem);

            return new ResLoginUsuario
            {
                Resultado = false,
                Error = errorList,
                Sesion = null
            };
        }

        #endregion

        #region Métodos de Verificación

        public async Task<ResVerificacion> VerificarUsuarioAsync(ReqVerificacion request)
        {
            try
            {
                // Serializar el objeto a JSON
                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Hacer la petición POST
                var response = await _httpClient.PostAsync("api/usuario/verificar", content);

                // Leer la respuesta
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        // Opciones de deserialización
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        };

                        var result = JsonSerializer.Deserialize<ResVerificacion>(responseContent, options);

                        return result ?? CreateVerificacionErrorResponse(-4, "Respuesta vacía del servidor");
                    }
                    catch (JsonException jsonEx)
                    {
                        return CreateVerificacionErrorResponse(-5, $"Error al procesar respuesta del servidor: {jsonEx.Message}");
                    }
                }
                else
                {
                    // Si hay error HTTP, intentar deserializar el error
                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        };

                        var errorResult = JsonSerializer.Deserialize<ResVerificacion>(responseContent, options);
                        return errorResult ?? CreateVerificacionErrorResponse((int)response.StatusCode, $"Error HTTP: {response.StatusCode}");
                    }
                    catch
                    {
                        return CreateVerificacionErrorResponse((int)response.StatusCode, $"Error del servidor: {responseContent}");
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                return CreateVerificacionErrorResponse(-1, $"Error de conexión: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                return CreateVerificacionErrorResponse(-2, "Tiempo de espera agotado");
            }
            catch (Exception ex)
            {
                return CreateVerificacionErrorResponse(-3, $"Error inesperado: {ex.Message}");
            }
        }

        private ResVerificacion CreateVerificacionErrorResponse(int errorCode, string message)
        {
            var errorList = new List<ErrorItem>();
            var errorItem = new ErrorItem
            {
                ErrorCode = errorCode,
                Message = message
            };
            errorList.Add(errorItem);

            return new ResVerificacion
            {
                Resultado = false,
                Error = errorList
            };
        }

        #endregion

        #region Métodos de Categorías y Servicios

        // Método para obtener categorías
        public async Task<ResListarCategorias> ObtenerCategoriasAsync()
        {
            try
            {
                // Hacer la petición GET
                var response = await _httpClient.GetAsync("api/categoria/listar");

                // Leer la respuesta
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        };

                        var result = JsonSerializer.Deserialize<ResListarCategorias>(responseContent, options);
                        return result ?? CreateCategoriasErrorResponse(-4, "Respuesta vacía del servidor");
                    }
                    catch (JsonException jsonEx)
                    {
                        return CreateCategoriasErrorResponse(-5, $"Error al procesar respuesta: {jsonEx.Message}");
                    }
                }
                else
                {
                    return CreateCategoriasErrorResponse((int)response.StatusCode, $"Error del servidor: {responseContent}");
                }
            }
            catch (HttpRequestException ex)
            {
                return CreateCategoriasErrorResponse(-1, $"Error de conexión: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                return CreateCategoriasErrorResponse(-2, "Tiempo de espera agotado");
            }
            catch (Exception ex)
            {
                return CreateCategoriasErrorResponse(-3, $"Error inesperado: {ex.Message}");
            }
        }

        private ResListarCategorias CreateCategoriasErrorResponse(int errorCode, string message)
        {
            return new ResListarCategorias
            {
                Resultado = false,
                Error = new List<ErrorItem> { new ErrorItem { ErrorCode = errorCode, Message = message } },
                Categorias = new List<Categoria>()
            };
        }

        // Método para obtener subcategorías
        public async Task<ResListarSubCategorias> ObtenerSubCategoriasAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/subcategoria/listar");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        };

                        var result = JsonSerializer.Deserialize<ResListarSubCategorias>(responseContent, options);
                        return result ?? CreateSubCategoriasErrorResponse(-4, "Respuesta vacía del servidor");
                    }
                    catch (JsonException jsonEx)
                    {
                        return CreateSubCategoriasErrorResponse(-5, $"Error al procesar respuesta: {jsonEx.Message}");
                    }
                }
                else
                {
                    return CreateSubCategoriasErrorResponse((int)response.StatusCode, $"Error del servidor: {responseContent}");
                }
            }
            catch (HttpRequestException ex)
            {
                return CreateSubCategoriasErrorResponse(-1, $"Error de conexión: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                return CreateSubCategoriasErrorResponse(-2, "Tiempo de espera agotado");
            }
            catch (Exception ex)
            {
                return CreateSubCategoriasErrorResponse(-3, $"Error inesperado: {ex.Message}");
            }
        }

        private ResListarSubCategorias CreateSubCategoriasErrorResponse(int errorCode, string message)
        {
            return new ResListarSubCategorias
            {
                Resultado = false,
                Error = new List<ErrorItem> { new ErrorItem { ErrorCode = errorCode, Message = message } },
                SubCategorias = new List<SubCategoria>()
            };
        }

        // Método para crear servicio
        public async Task<ResInsertarServicio> CrearServicioAsync(ReqInsertarServicio request)
        {
            try
            {
                // Verificar sesión
                if (!SessionManager.EstaLogueado())
                {
                    return CreateServicioErrorResponse(-10, "No hay sesión activa");
                }

                // Configurar autenticación
                ConfigurarAutenticacion();

                // Asegurar que el SesionId esté en el request
                request.SesionId = SessionManager.ObtenerSessionId();

                // Serializar el objeto a JSON
                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                // Log para debugging
                System.Diagnostics.Debug.WriteLine($"JSON enviado: {json}");

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Hacer la petición POST
                var response = await _httpClient.PostAsync("api/servicio/insertar", content);

                // Leer la respuesta
                var responseContent = await response.Content.ReadAsStringAsync();

                // Log para debugging
                System.Diagnostics.Debug.WriteLine($"Response: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        };

                        var result = JsonSerializer.Deserialize<ResInsertarServicio>(responseContent, options);
                        return result ?? CreateServicioErrorResponse(-4, "Respuesta vacía del servidor");
                    }
                    catch (JsonException jsonEx)
                    {
                        return CreateServicioErrorResponse(-5, $"Error al procesar respuesta: {jsonEx.Message}");
                    }
                }
                else
                {
                    // Si es error 401, la sesión puede haber expirado
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        SessionManager.CerrarSesion();
                        return CreateServicioErrorResponse(-12, "Sesión inválida. Por favor, inicia sesión nuevamente.");
                    }

                    // Intentar deserializar el error
                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        };

                        var errorResult = JsonSerializer.Deserialize<ResInsertarServicio>(responseContent, options);
                        if (errorResult != null)
                        {
                            return errorResult;
                        }
                    }
                    catch
                    {
                        // Si no se puede deserializar, crear error genérico
                    }

                    return CreateServicioErrorResponse((int)response.StatusCode, $"Error del servidor ({response.StatusCode}): {responseContent}");
                }
            }
            catch (HttpRequestException ex)
            {
                return CreateServicioErrorResponse(-1, $"Error de conexión: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                return CreateServicioErrorResponse(-2, "Tiempo de espera agotado");
            }
            catch (Exception ex)
            {
                return CreateServicioErrorResponse(-3, $"Error inesperado: {ex.Message}");
            }
        }

        private ResInsertarServicio CreateServicioErrorResponse(int errorCode, string message)
        {
            return new ResInsertarServicio
            {
                Resultado = false,
                Error = new List<ErrorItem> { new ErrorItem { ErrorCode = errorCode, Message = message } },
                ServicioId = 0,
                Mensaje = ""
            };
        }

        #endregion

        #region Métodos de Ubicación

        // Método para obtener provincias desde la base de datos
        public async Task<ResListarProvincias> ObtenerProvinciasAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/provincia/listar");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        };

                        var result = JsonSerializer.Deserialize<ResListarProvincias>(responseContent, options);
                        return result ?? CreateProvinciasErrorResponse(-4, "Respuesta vacía del servidor");
                    }
                    catch (JsonException jsonEx)
                    {
                        return CreateProvinciasErrorResponse(-5, $"Error al procesar respuesta: {jsonEx.Message}");
                    }
                }
                else
                {
                    return CreateProvinciasErrorResponse((int)response.StatusCode, $"Error del servidor: {responseContent}");
                }
            }
            catch (HttpRequestException ex)
            {
                return CreateProvinciasErrorResponse(-1, $"Error de conexión: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                return CreateProvinciasErrorResponse(-2, "Tiempo de espera agotado");
            }
            catch (Exception ex)
            {
                return CreateProvinciasErrorResponse(-3, $"Error inesperado: {ex.Message}");
            }
        }

        private ResListarProvincias CreateProvinciasErrorResponse(int errorCode, string message)
        {
            return new ResListarProvincias
            {
                Resultado = false,
                Error = new List<ErrorItem> { new ErrorItem { ErrorCode = errorCode, Message = message } },
                Provincias = new List<Provincia>()
            };
        }

        // Método para obtener cantones desde la base de datos
        public async Task<ResListarCantones> ObtenerCantonesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/canton/listar");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        };

                        var result = JsonSerializer.Deserialize<ResListarCantones>(responseContent, options);
                        return result ?? CreateCantonesErrorResponse(-4, "Respuesta vacía del servidor");
                    }
                    catch (JsonException jsonEx)
                    {
                        return CreateCantonesErrorResponse(-5, $"Error al procesar respuesta: {jsonEx.Message}");
                    }
                }
                else
                {
                    return CreateCantonesErrorResponse((int)response.StatusCode, $"Error del servidor: {responseContent}");
                }
            }
            catch (HttpRequestException ex)
            {
                return CreateCantonesErrorResponse(-1, $"Error de conexión: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                return CreateCantonesErrorResponse(-2, "Tiempo de espera agotado");
            }
            catch (Exception ex)
            {
                return CreateCantonesErrorResponse(-3, $"Error inesperado: {ex.Message}");
            }
        }

        private ResListarCantones CreateCantonesErrorResponse(int errorCode, string message)
        {
            return new ResListarCantones
            {
                Resultado = false,
                Error = new List<ErrorItem> { new ErrorItem { ErrorCode = errorCode, Message = message } },
                Cantones = new List<Canton>()
            };
        }

        // Método auxiliar para filtrar cantones por provincia (en el cliente)
        public List<Canton> FiltrarCantonesPorProvincia(List<Canton> cantones, int provinciaId)
        {
            return cantones?.Where(c => c.Provincia?.ProvinciaId == provinciaId).ToList() ?? new List<Canton>();
        }

        #endregion

        #region Cleanup

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        #endregion

        #region Métodos para Listar Servicios

        // Método para obtener todos los servicios
        public async Task<ResListarServicios> ObtenerServiciosAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔍 Obteniendo lista de servicios...");

                // Hacer la petición GET
                var response = await _httpClient.GetAsync("api/servicio/listar");

                // Leer la respuesta
                var responseContent = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"📊 Response Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"📄 Response Content: {responseContent.Substring(0, Math.Min(500, responseContent.Length))}...");

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        };

                        var result = JsonSerializer.Deserialize<ResListarServicios>(responseContent, options);

                        if (result == null)
                        {
                            return CreateListarServiciosErrorResponse(-4, "Respuesta vacía del servidor");
                        }

                        System.Diagnostics.Debug.WriteLine($"✅ Servicios obtenidos: {result.Servicios?.Count ?? 0}");
                        return result;
                    }
                    catch (JsonException jsonEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Error JSON: {jsonEx.Message}");
                        return CreateListarServiciosErrorResponse(-5, $"Error al procesar respuesta: {jsonEx.Message}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error HTTP: {response.StatusCode}");
                    return CreateListarServiciosErrorResponse((int)response.StatusCode, $"Error del servidor: {responseContent}");
                }
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error de conexión: {ex.Message}");
                return CreateListarServiciosErrorResponse(-1, $"Error de conexión: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Timeout: {ex.Message}");
                return CreateListarServiciosErrorResponse(-2, "Tiempo de espera agotado");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error inesperado: {ex.Message}");
                return CreateListarServiciosErrorResponse(-3, $"Error inesperado: {ex.Message}");
            }
        }

        private ResListarServicios CreateListarServiciosErrorResponse(int errorCode, string message)
        {
            return new ResListarServicios
            {
                Resultado = false,
                Error = new List<ErrorItem> { new ErrorItem { ErrorCode = errorCode, Message = message } },
                Servicios = new List<Servicio>()
            };
        }

        #endregion

        #region Método para Obtener Perfil (Nueva API)

        // Nuevo método usando la API obtenerPerfil con SesionId
        public async Task<ResObtenerPerfil> ObtenerPerfilUsuarioAsync()
        {
            try
            {
                // Verificar si hay sesión activa
                if (!SessionManager.EstaLogueado())
                {
                    return CreateObtenerPerfilErrorResponse(-10, "No hay sesión activa. Por favor, inicia sesión.");
                }

                // Configurar autenticación
                ConfigurarAutenticacion();

                // Obtener SesionId
                var sesionId = SessionManager.ObtenerSessionId();

                if (string.IsNullOrEmpty(sesionId))
                {
                    return CreateObtenerPerfilErrorResponse(-11, "No se encontró información de sesión.");
                }

                // Crear el request con el SesionId
                var request = new ReqObtenerPerfil
                {
                    SesionId = sesionId
                };

                // Serializar el objeto a JSON
                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                // Log para debugging
                System.Diagnostics.Debug.WriteLine($"🔄 Obteniendo perfil con SesionId: {sesionId}");
                System.Diagnostics.Debug.WriteLine($"📄 Request JSON: {json}");

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Hacer la petición POST a la nueva API
                var response = await _httpClient.PostAsync("api/usuario/obtenerPerfil", content);

                // Leer la respuesta
                var responseContent = await response.Content.ReadAsStringAsync();

                // Log para debugging
                System.Diagnostics.Debug.WriteLine($"📊 Response Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"📄 Response Content: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        };

                        var result = JsonSerializer.Deserialize<ResObtenerPerfil>(responseContent, options);

                        if (result == null)
                        {
                            return CreateObtenerPerfilErrorResponse(-4, "Respuesta vacía del servidor");
                        }

                        System.Diagnostics.Debug.WriteLine("✅ Perfil obtenido exitosamente");
                        return result;
                    }
                    catch (JsonException jsonEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Error JSON: {jsonEx.Message}");
                        return CreateObtenerPerfilErrorResponse(-5, $"Error al procesar respuesta del servidor: {jsonEx.Message}");
                    }
                }
                else
                {
                    // Si es error 401 (No autorizado), la sesión puede haber expirado
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        SessionManager.CerrarSesion();
                        return CreateObtenerPerfilErrorResponse(-12, "Sesión inválida. Por favor, inicia sesión nuevamente.");
                    }

                    // Intentar deserializar el error
                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        };

                        var errorResult = JsonSerializer.Deserialize<ResObtenerPerfil>(responseContent, options);
                        if (errorResult != null)
                        {
                            return errorResult;
                        }
                    }
                    catch
                    {
                        // Si no se puede deserializar, crear error genérico
                    }

                    return CreateObtenerPerfilErrorResponse((int)response.StatusCode, $"Error del servidor ({response.StatusCode}): {responseContent}");
                }
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error de conexión: {ex.Message}");
                return CreateObtenerPerfilErrorResponse(-1, $"Error de conexión: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Timeout: {ex.Message}");
                return CreateObtenerPerfilErrorResponse(-2, "Tiempo de espera agotado");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error inesperado: {ex.Message}");
                return CreateObtenerPerfilErrorResponse(-3, $"Error inesperado: {ex.Message}");
            }
        }

        private ResObtenerPerfil CreateObtenerPerfilErrorResponse(int errorCode, string message)
        {
            var errorList = new List<ErrorItem>();
            var errorItem = new ErrorItem
            {
                ErrorCode = errorCode,
                Message = message
            };
            errorList.Add(errorItem);

            return new ResObtenerPerfil
            {
                Resultado = false,
                Error = errorList,
                Usuario = null
            };
        }

        #endregion

        // Agregar este método a FrontMoviles/Servicios/ApiService.cs

        #region Método para Actualizar Datos de Usuario

        public async Task<ResActualizarDatosUsuario> ActualizarDatosUsuarioAsync(ReqActualizarDatosUsuario request)
        {
            try
            {
                // Verificar si hay sesión activa
                if (!SessionManager.EstaLogueado())
                {
                    return CreateActualizarDatosErrorResponse(-10, "No hay sesión activa. Por favor, inicia sesión.");
                }

                // Configurar autenticación
                ConfigurarAutenticacion();

                // Asegurar que el SesionId esté en el request
                request.SesionId = SessionManager.ObtenerSessionId();

                // Serializar el objeto a JSON
                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                // Log para debugging
                System.Diagnostics.Debug.WriteLine($"🔄 Actualizando datos de usuario...");
                System.Diagnostics.Debug.WriteLine($"📄 Request JSON: {json}");

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Hacer la petición POST
                var response = await _httpClient.PostAsync("api/usuario/actualizar", content);

                // Leer la respuesta
                var responseContent = await response.Content.ReadAsStringAsync();

                // Log para debugging
                System.Diagnostics.Debug.WriteLine($"📊 Response Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"📄 Response Content: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        };

                        var result = JsonSerializer.Deserialize<ResActualizarDatosUsuario>(responseContent, options);

                        if (result == null)
                        {
                            return CreateActualizarDatosErrorResponse(-4, "Respuesta vacía del servidor");
                        }

                        System.Diagnostics.Debug.WriteLine("✅ Datos actualizados exitosamente");
                        return result;
                    }
                    catch (JsonException jsonEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Error JSON: {jsonEx.Message}");
                        return CreateActualizarDatosErrorResponse(-5, $"Error al procesar respuesta del servidor: {jsonEx.Message}");
                    }
                }
                else
                {
                    // Si es error 401, la sesión puede haber expirado
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        SessionManager.CerrarSesion();
                        return CreateActualizarDatosErrorResponse(-12, "Sesión inválida. Por favor, inicia sesión nuevamente.");
                    }

                    // Intentar deserializar el error
                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        };

                        var errorResult = JsonSerializer.Deserialize<ResActualizarDatosUsuario>(responseContent, options);
                        if (errorResult != null)
                        {
                            return errorResult;
                        }
                    }
                    catch
                    {
                        // Si no se puede deserializar, crear error genérico
                    }

                    return CreateActualizarDatosErrorResponse((int)response.StatusCode, $"Error del servidor ({response.StatusCode}): {responseContent}");
                }
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error de conexión: {ex.Message}");
                return CreateActualizarDatosErrorResponse(-1, $"Error de conexión: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Timeout: {ex.Message}");
                return CreateActualizarDatosErrorResponse(-2, "Tiempo de espera agotado");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error inesperado: {ex.Message}");
                return CreateActualizarDatosErrorResponse(-3, $"Error inesperado: {ex.Message}");
            }
        }

        private ResActualizarDatosUsuario CreateActualizarDatosErrorResponse(int errorCode, string message)
        {
            return new ResActualizarDatosUsuario
            {
                Resultado = false,
                Error = new List<ErrorItem> { new ErrorItem { ErrorCode = errorCode, Message = message } }
            };
        }

        #endregion


        // Agregar estos métodos a FrontMoviles/Servicios/ApiService.cs

        #region Métodos de Reseñas

        // Método para insertar una reseña
        public async Task<ResInsertarResena> InsertarResenaAsync(ReqInsertarResena request)
        {
            try
            {
                // Verificar si hay sesión activa
                if (!SessionManager.EstaLogueado())
                {
                    return CreateInsertarResenaErrorResponse("No hay sesión activa. Por favor, inicia sesión.");
                }

                // Configurar autenticación
                ConfigurarAutenticacion();

                // Asegurar que el SesionId esté en el request
                request.SesionId = SessionManager.ObtenerSessionId();

                // Serializar el objeto a JSON
                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                // Log para debugging
                System.Diagnostics.Debug.WriteLine($"🌟 Insertando reseña...");
                System.Diagnostics.Debug.WriteLine($"📄 Request JSON: {json}");

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Hacer la petición POST
                var response = await _httpClient.PostAsync("api/resena/insertarResena", content);

                // Leer la respuesta
                var responseContent = await response.Content.ReadAsStringAsync();

                // Log para debugging
                System.Diagnostics.Debug.WriteLine($"📊 Response Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"📄 Response Content: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        };

                        var result = JsonSerializer.Deserialize<ResInsertarResena>(responseContent, options);

                        if (result == null)
                        {
                            return CreateInsertarResenaErrorResponse("Respuesta vacía del servidor");
                        }

                        System.Diagnostics.Debug.WriteLine("✅ Reseña insertada exitosamente");
                        return result;
                    }
                    catch (JsonException jsonEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Error JSON: {jsonEx.Message}");
                        return CreateInsertarResenaErrorResponse($"Error al procesar respuesta del servidor: {jsonEx.Message}");
                    }
                }
                else
                {
                    // Si es error 401, la sesión puede haber expirado
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        SessionManager.CerrarSesion();
                        return CreateInsertarResenaErrorResponse("Sesión inválida. Por favor, inicia sesión nuevamente.");
                    }

                    // Intentar deserializar el error
                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        };

                        var errorResult = JsonSerializer.Deserialize<ResInsertarResena>(responseContent, options);
                        if (errorResult != null)
                        {
                            return errorResult;
                        }
                    }
                    catch
                    {
                        // Si no se puede deserializar, crear error genérico
                    }

                    return CreateInsertarResenaErrorResponse($"Error del servidor ({response.StatusCode}): {responseContent}");
                }
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error de conexión: {ex.Message}");
                return CreateInsertarResenaErrorResponse($"Error de conexión: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Timeout: {ex.Message}");
                return CreateInsertarResenaErrorResponse("Tiempo de espera agotado");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error inesperado: {ex.Message}");
                return CreateInsertarResenaErrorResponse($"Error inesperado: {ex.Message}");
            }
        }

        // Método para eliminar una reseña
        public async Task<ResEliminarResena> EliminarResenaAsync(ReqEliminarResena request)
        {
            try
            {
                // Verificar si hay sesión activa
                if (!SessionManager.EstaLogueado())
                {
                    return CreateEliminarResenaErrorResponse("No hay sesión activa. Por favor, inicia sesión.");
                }

                // Configurar autenticación
                ConfigurarAutenticacion();

                // Serializar el objeto a JSON
                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                // Log para debugging
                System.Diagnostics.Debug.WriteLine($"🗑️ Eliminando reseña...");
                System.Diagnostics.Debug.WriteLine($"📄 Request JSON: {json}");

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Hacer la petición POST
                var response = await _httpClient.PostAsync("api/resena/eliminarResena", content);

                // Leer la respuesta
                var responseContent = await response.Content.ReadAsStringAsync();

                // Log para debugging
                System.Diagnostics.Debug.WriteLine($"📊 Response Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"📄 Response Content: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        };

                        var result = JsonSerializer.Deserialize<ResEliminarResena>(responseContent, options);

                        if (result == null)
                        {
                            return CreateEliminarResenaErrorResponse("Respuesta vacía del servidor");
                        }

                        System.Diagnostics.Debug.WriteLine("✅ Reseña eliminada exitosamente");
                        return result;
                    }
                    catch (JsonException jsonEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Error JSON: {jsonEx.Message}");
                        return CreateEliminarResenaErrorResponse($"Error al procesar respuesta del servidor: {jsonEx.Message}");
                    }
                }
                else
                {
                    // Si es error 401, la sesión puede haber expirado
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        SessionManager.CerrarSesion();
                        return CreateEliminarResenaErrorResponse("Sesión inválida. Por favor, inicia sesión nuevamente.");
                    }

                    // Intentar deserializar el error
                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        };

                        var errorResult = JsonSerializer.Deserialize<ResEliminarResena>(responseContent, options);
                        if (errorResult != null)
                        {
                            return errorResult;
                        }
                    }
                    catch
                    {
                        // Si no se puede deserializar, crear error genérico
                    }

                    return CreateEliminarResenaErrorResponse($"Error del servidor ({response.StatusCode}): {responseContent}");
                }
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error de conexión: {ex.Message}");
                return CreateEliminarResenaErrorResponse($"Error de conexión: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Timeout: {ex.Message}");
                return CreateEliminarResenaErrorResponse("Tiempo de espera agotado");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error inesperado: {ex.Message}");
                return CreateEliminarResenaErrorResponse($"Error inesperado: {ex.Message}");
            }
        }

        // Métodos auxiliares para crear respuestas de error
        private ResInsertarResena CreateInsertarResenaErrorResponse(string message)
        {
            return new ResInsertarResena
            {
                Resultado = false,
                Mensaje = "",
                Error = new List<ErrorItem> { new ErrorItem { ErrorCode = -1, Message = message } }
            };
        }

        private ResEliminarResena CreateEliminarResenaErrorResponse(string message)
        {
            return new ResEliminarResena
            {
                Resultado = false,
                Mensaje = "",
                Error = new List<ErrorItem> { new ErrorItem { ErrorCode = -1, Message = message } }
            };
        }

        #endregion


        // Agregar este método en la sección de Reseñas en FrontMoviles/Servicios/ApiService.cs

        #region Método para Listar Reseñas por Servicio

        // Método para obtener reseñas de un servicio específico
        // Completar el método en FrontMoviles/Servicios/ApiService.cs

        public async Task<ResListarResenasPorServicio> ObtenerResenasPorServicioAsync(Servicio servicio)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔍 Obteniendo reseñas para servicio: {servicio.Titulo} (ID: {servicio.ServicioId})");

                // Crear el request
                var request = new ReqListarResenasPorServicio
                {
                    Servicio = servicio
                };

                // Serializar el objeto a JSON
                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                System.Diagnostics.Debug.WriteLine($"📄 Request JSON: {json}");

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Hacer la petición POST
                var response = await _httpClient.PostAsync("api/resena/listarResenasPorServicio", content);

                // Leer la respuesta
                var responseContent = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"📊 Response Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"📄 Response Content: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        };

                        var result = JsonSerializer.Deserialize<ResListarResenasPorServicio>(responseContent, options);

                        if (result == null)
                        {
                            return CreateListarResenasErrorResponse("Respuesta vacía del servidor");
                        }

                        System.Diagnostics.Debug.WriteLine($"✅ Reseñas obtenidas: {result.Resenas?.Count ?? 0}");
                        return result;
                    }
                    catch (JsonException ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Error deserializando respuesta: {ex.Message}");
                        return CreateListarResenasErrorResponse($"Error procesando respuesta: {ex.Message}");
                    }
                }
                else
                {
                    // Manejar errores HTTP
                    string errorMessage = $"Error del servidor ({response.StatusCode})";

                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<ResListarResenasPorServicio>(responseContent, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (errorResponse?.Error?.Any() == true)
                        {
                            errorMessage = errorResponse.Error.First().Message;
                        }
                    }
                    catch
                    {
                        // Si no se puede deserializar el error, usar mensaje genérico
                    }

                    System.Diagnostics.Debug.WriteLine($"❌ Error HTTP: {errorMessage}");
                    return CreateListarResenasErrorResponse(errorMessage);
                }
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error de conexión: {ex.Message}");
                return CreateListarResenasErrorResponse($"Error de conexión: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Timeout: {ex.Message}");
                return CreateListarResenasErrorResponse("Tiempo de espera agotado");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error inesperado: {ex.Message}");
                return CreateListarResenasErrorResponse($"Error inesperado: {ex.Message}");
            }
        }

        private ResListarResenasPorServicio CreateListarResenasErrorResponse(string message)
        {
            return new ResListarResenasPorServicio
            {
                Resultado = false,
                Resenas = new List<Resena>(),
                Error = new List<ErrorItem> { new ErrorItem { ErrorCode = -1, Message = message } }
            };
        }
        #endregion
    }
}