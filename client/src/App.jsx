import { useState, useEffect } from "react";

export default function App() {
  const [token, setToken] = useState("");
  const [file, setFile] = useState(null);
  const [status, setStatus] = useState("");
  const [pictureUrl, setPictureUrl] = useState("");

  // 1. Fetch CSRF token and cookie on page load
  useEffect(() => {
    fetch("http://localhost:5159/api/antiforgery/token", {
      credentials: "include", // Attaches and receives cookies across ports
    })
      .then((res) => res.json())
      .then((data) => {
        setToken(data.token);
        setStatus("CSRF token loaded and cookie set.");
      })
      .catch((err) => setStatus("Error fetching CSRF token: " + err.message));
  }, []);

  // 2. Submit the image to POST /api/profile/photo
  const handleUpload = async (e) => {
    e.preventDefault();
    if (!file) {
      setStatus("Please select a file first.");
      return;
    }

    const formData = new FormData();
    formData.append("file", file); // Must match IFormFile parameter name: "file"

    setStatus("Uploading...");

    try {
      const res = await fetch("http://localhost:5159/api/profile/photo", {
        method: "POST",
        credentials: "include", // Automatically sends the Antiforgery cookie
        headers: {
          "X-Xsrf-Token": token, // Attaches the custom CSRF header
        },
        body: formData,
      });

      const data = await res.json();

      if (res.ok) {
        setStatus("Upload successful!");
        // Extract UUID and construct local Azurite link to display image
        const fileName = data.pictureUrl.split("/").pop();
        setPictureUrl(
          `http://127.0.0.1:10000/devstoreaccount1/user-images/${fileName}`,
        );
      } else {
        setStatus(`Upload failed: ${data.message || "Server error"}`);
      }
    } catch (err) {
      setStatus("Network error: " + err.message);
    }
  };

  return (
    <div style={{ padding: "2rem", fontFamily: "sans-serif" }}>
      <h2>Octopus Profile Photo Uploader</h2>
      <p style={{ color: "#555" }}>
        Status: <strong>{status}</strong>
      </p>

      <form onSubmit={handleUpload}>
        <input
          type="file"
          accept="image/jpeg,image/png,image/gif"
          onChange={(e) => setFile(e.target.files[0])}
        />
        <button
          type="submit"
          style={{ marginLeft: "1rem", padding: "0.4rem 1rem" }}
        >
          Upload Photo
        </button>
      </form>

      {pictureUrl && (
        <div style={{ marginTop: "2rem" }}>
          <h4>Uploaded Photo (Rendered from Local Azurite Storage):</h4>
          <img
            src={pictureUrl}
            alt="Uploaded Profile"
            style={{
              maxWidth: "300px",
              borderRadius: "8px",
              border: "1px solid #ccc",
            }}
          />
          <p style={{ fontSize: "0.8rem", color: "#666" }}>{pictureUrl}</p>
        </div>
      )}
    </div>
  );
}
