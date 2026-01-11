Imports System.IO
Imports System.Net
Imports System.Text
Imports System.Text.Json
Imports System.Threading.Tasks
Imports Npgsql
Imports NpgsqlTypes

Module Program
    Private ReadOnly ListenerPrefix = "http://localhost:5000/"
    Private ReadOnly DefaultConnectionString =
        "Host=localhost;Port=5432;Database=nutriciondb;Username=postgres;Password=postgres;Pooling=true;Trust Server Certificate=true"

    Private ReadOnly Property ConnectionString As String
        Get
            Return If(Environment.GetEnvironmentVariable("NUTRITION_DB"), DefaultConnectionString)
        End Get
    End Property

    Sub Main(args As String())
        Dim listener = New HttpListener()
        listener.Prefixes.Add(ListenerPrefix)
        listener.Start()
        Console.WriteLine($"Servidor en {ListenerPrefix} · Base de datos: {ConnectionString}")

        AddHandler Console.CancelKeyPress, Sub(sender, e)
                                               e.Cancel = True
                                               listener.Stop()
                                           End Sub

        RunServerAsync(listener).GetAwaiter().GetResult()
    End Sub

    Private Async Function RunServerAsync(listener As HttpListener) As Task
        While listener.IsListening
            Try
                Dim context = Await listener.GetContextAsync()
                Await ProcessRequestAsync(context)
            Catch ex As HttpListenerException
                Exit While
            Catch ex As Exception
                Console.Error.WriteLine(ex)
            End Try
        End While
    End Function

    Private Async Function ProcessRequestAsync(context As HttpListenerContext) As Task
        Dim request = context.Request
        Dim response = context.Response

        Try
            AddCorsHeaders(response)

            If request.HttpMethod = "OPTIONS" Then
                response.StatusCode = 204
                Return
            End If

            Dim path = request.Url.AbsolutePath.TrimEnd("/"c).ToLowerInvariant()
            Select Case path
                Case "/api/nutrition/history"
                    If request.HttpMethod = "POST" Then
                        Await HandleSaveHistoryAsync(request, response)
                    Else
                        WriteError(response, 405, "Método no permitido")
                    End If
                Case "/api/nutrition/status"
                    If request.HttpMethod = "GET" Then
                        response.StatusCode = 200
                        response.ContentType = "application/json"
                        Dim payload = JsonSerializer.Serialize(New With {Key .status = "running"})
                        Dim buffer = Encoding.UTF8.GetBytes(payload)
                        Await response.OutputStream.WriteAsync(buffer, 0, buffer.Length)
                    Else
                        WriteError(response, 405, "Método no permitido")
                    End If
                Case Else
                    WriteError(response, 404, "Endpoint no encontrado")
            End Select
        Catch ex As Exception
            WriteError(response, 500, ex.Message)
        Finally
            response.Close()
        End Try
    End Function

    Private Async Function HandleSaveHistoryAsync(request As HttpListenerRequest, response As HttpListenerResponse) As Task
        Dim payload As String
        Using reader = New StreamReader(request.InputStream, request.ContentEncoding)
            payload = Await reader.ReadToEndAsync()
        End Using

        If String.IsNullOrWhiteSpace(payload) Then
            WriteError(response, 400, "Payload vacío")
            Return
        End If

        Try
            Await SaveRecordAsync(payload)
            response.StatusCode = 201
            response.ContentType = "application/json"
            Dim payloadResponse = JsonSerializer.Serialize(New With {Key .status = "created"})
            Dim buffer = Encoding.UTF8.GetBytes(payloadResponse)
            Await response.OutputStream.WriteAsync(buffer, 0, buffer.Length)
        Catch ex As JsonException
            WriteError(response, 400, "JSON inválido")
        Catch ex As Exception
            WriteError(response, 500, $"Error al guardar: {ex.Message}")
        End Try
    End Function

    Private Async Function SaveRecordAsync(jsonPayload As String) As Task
        Using connection As New NpgsqlConnection(ConnectionString)
            Await connection.OpenAsync()
            Await EnsureSchemaAsync(connection)

            Dim command = New NpgsqlCommand(
                "INSERT INTO clinical_histories (id, payload) VALUES (@id, @payload)",
                connection)

            command.Parameters.Add(New NpgsqlParameter("id", NpgsqlDbType.Uuid) With {
                .Value = Guid.NewGuid()
            })

            Using document = JsonDocument.Parse(jsonPayload)
                command.Parameters.Add(New NpgsqlParameter("payload", NpgsqlDbType.Jsonb) With {
                    .Value = document.RootElement.Clone()
                })
            End Using

            Await command.ExecuteNonQueryAsync()
        End Using
    End Function

    Private Async Function EnsureSchemaAsync(connection As NpgsqlConnection) As Task
        Dim script = "
CREATE TABLE IF NOT EXISTS clinical_histories (
  id uuid PRIMARY KEY,
  payload jsonb NOT NULL,
  recorded_at timestamptz NOT NULL DEFAULT NOW()
);"

        Dim command = New NpgsqlCommand(script, connection)
        Await command.ExecuteNonQueryAsync()
    End Function

    Private Sub AddCorsHeaders(response As HttpListenerResponse)
        response.AddHeader("Access-Control-Allow-Origin", "*")
        response.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS")
        response.AddHeader("Access-Control-Allow-Headers", "Content-Type")
    End Sub

    Private Sub WriteError(response As HttpListenerResponse, statusCode As Integer, message As String)
        response.StatusCode = statusCode
        response.ContentType = "application/json"
        Dim payload = JsonSerializer.Serialize(New With {Key .error = message})
        Dim buffer = Encoding.UTF8.GetBytes(payload)
        response.OutputStream.Write(buffer, 0, buffer.Length)
    End Sub
End Module
