using System;
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
        private const string BASE_URL = "http://localhost:56387/"; // Cambia por tu URL base

        public ApiService()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(BASE_URL);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

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

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

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

                    return result ?? CreateErrorResponse(-4, "Respuesta vacía del servidor");
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
                        return errorResult ?? CreateErrorResponse((int)response.StatusCode, $"Error HTTP: {response.StatusCode}");
                    }
                    catch
                    {
                        return CreateErrorResponse((int)response.StatusCode, responseContent);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                return CreateErrorResponse(-1, $"Error de conexión: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                return CreateErrorResponse(-2, "Tiempo de espera agotado");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(-3, $"Error inesperado: {ex.Message}");
            }
        }

        private ResInsertarUsuario CreateErrorResponse(int errorCode, string message)
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

        // Método para obtener provincias desde la base de datos
        public async Task<List<Provincia>> ObtenerProvinciasAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/provincia/obtener");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    // Si tu API devuelve directamente una lista
                    var provincias = JsonSerializer.Deserialize<List<Provincia>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return provincias ?? new List<Provincia>();
                }
                else
                {
                    // Si falla, usar datos mock como respaldo
                    return GetMockProvincias();
                }
            }
            catch (Exception ex)
            {
                // En caso de error, usar datos mock
                return GetMockProvincias();
            }
        }

        private List<Provincia> GetMockProvincias()
        {
            var provincias = new List<Provincia>();
            provincias.Add(new Provincia { ProvinciaId = 1, Nombre = "San José" });
            provincias.Add(new Provincia { ProvinciaId = 2, Nombre = "Alajuela" });
            provincias.Add(new Provincia { ProvinciaId = 3, Nombre = "Cartago" });
            provincias.Add(new Provincia { ProvinciaId = 4, Nombre = "Heredia" });
            provincias.Add(new Provincia { ProvinciaId = 5, Nombre = "Guanacaste" });
            provincias.Add(new Provincia { ProvinciaId = 6, Nombre = "Puntarenas" });
            provincias.Add(new Provincia { ProvinciaId = 7, Nombre = "Limón" });
            return provincias;
        }

        // Método para obtener cantones desde la base de datos
        public async Task<List<Canton>> ObtenerCantonesPorProvinciaAsync(int provinciaId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/canton/obtener/{provinciaId}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    // Si tu API devuelve directamente una lista
                    var cantones = JsonSerializer.Deserialize<List<Canton>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return cantones ?? new List<Canton>();
                }
                else
                {
                    // Si falla, usar datos mock como respaldo
                    return GetMockCantones(provinciaId);
                }
            }
            catch (Exception ex)
            {
                // En caso de error, usar datos mock
                return GetMockCantones(provinciaId);
            }
        }

        private List<Canton> GetMockCantones(int provinciaId)
        {
            var cantones = new List<Canton>();

            if (provinciaId == 1)
            {
                var provincia = new Provincia { ProvinciaId = 1, Nombre = "San José" };
                cantones.Add(new Canton { CantonId = 1, Nombre = "San José", Provincia = provincia });
                cantones.Add(new Canton { CantonId = 2, Nombre = "Escazú", Provincia = provincia });
                cantones.Add(new Canton { CantonId = 3, Nombre = "Desamparados", Provincia = provincia });
            }

            return cantones;
        }

        // Método para obtener distritos desde la base de datos  
        public async Task<List<Distrito>> ObtenerDistritosPorCantonAsync(int cantonId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/distrito/obtener/{cantonId}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    // Si tu API devuelve directamente una lista
                    var distritos = JsonSerializer.Deserialize<List<Distrito>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return distritos ?? new List<Distrito>();
                }
                else
                {
                    // Si falla, usar datos mock como respaldo
                    return GetMockDistritos(cantonId);
                }
            }
            catch (Exception ex)
            {
                // En caso de error, usar datos mock
                return GetMockDistritos(cantonId);
            }
        }

        private List<Distrito> GetMockDistritos(int cantonId)
        {
            var distritos = new List<Distrito>();

            if (cantonId == 1) // San José
            {
                var provincia = new Provincia { ProvinciaId = 1, Nombre = "San José" };
                var canton = new Canton { CantonId = 1, Nombre = "San José", Provincia = provincia };

                distritos.Add(new Distrito { DistritoId = 1, Nombre = "Carmen", Canton = canton });
                distritos.Add(new Distrito { DistritoId = 2, Nombre = "Merced", Canton = canton });
                distritos.Add(new Distrito { DistritoId = 3, Nombre = "Hospital", Canton = canton });
                distritos.Add(new Distrito { DistritoId = 4, Nombre = "Catedral", Canton = canton });
            }
            else if (cantonId == 2) // Escazú
            {
                var provincia = new Provincia { ProvinciaId = 1, Nombre = "San José" };
                var canton = new Canton { CantonId = 2, Nombre = "Escazú", Provincia = provincia };

                distritos.Add(new Distrito { DistritoId = 5, Nombre = "Escazú", Canton = canton });
                distritos.Add(new Distrito { DistritoId = 6, Nombre = "San Antonio", Canton = canton });
                distritos.Add(new Distrito { DistritoId = 7, Nombre = "San Rafael", Canton = canton });
            }

            return distritos;
        }
    }
}