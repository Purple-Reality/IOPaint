import { API_ENDPOINT } from "@/lib/api"
import { useCallback, useEffect, useState } from "react"

export default function useInputImage() {
  const [inputImage, setInputImage] = useState<File | null>(null)

  const fetchInputImage = useCallback(() => {
    console.log("=== useInputImage fetchInputImage called ===");
    console.log("Current URL:", window.location.href);
    console.log("Search params:", window.location.search);
    console.log("API_ENDPOINT:", API_ENDPOINT);
    
    // Vérifier si un paramètre ?image=... est présent dans l'URL
    const params = new URLSearchParams(window.location.search)
    const imageParam = params.get("image")
    
    console.log("Image parameter from URL:", imageParam);
    
    if (imageParam) {
      console.log("📸 Image parameter found, attempting to load image...");
      
      // Construire l'URL complète
      const imageUrl = `/api/v1/cached_image/${imageParam}`;
      console.log("🔗 Constructed image URL:", imageUrl);
      
      // Charger l'image depuis le dossier de sortie
      console.log("🚀 Starting fetch request...");
      fetch(imageUrl)
        .then(async (res) => {
          console.log("📡 Fetch response received:");
          console.log("- Status:", res.status);
          console.log("- Status text:", res.statusText);
          console.log("- OK:", res.ok);
          console.log("- Headers:", Object.fromEntries(res.headers.entries()));
          
          if (!res.ok) {
            console.error("❌ Response not OK, aborting");
            console.error("Response status:", res.status);
            console.error("Response text:", await res.text());
            return;
          }
          
          console.log("✅ Response OK, getting blob...");
          const data = await res.blob()
          console.log("📦 Blob received:");
          console.log("- Size:", data.size, "bytes");
          console.log("- Type:", data.type);
          
          if (data && data.type.startsWith("image")) {
            console.log("🖼️ Valid image blob, creating File object...");
            const userInput = new File([data], imageParam)
            console.log("📄 File created:");
            console.log("- Name:", userInput.name);
            console.log("- Size:", userInput.size);
            console.log("- Type:", userInput.type);
            console.log("- Last modified:", userInput.lastModified);
            
            console.log("💾 Setting input image...");
            setInputImage(userInput)
            console.log("✅ Image successfully loaded from URL parameter!");
          } else {
            console.error("❌ Invalid blob or not an image");
            console.error("- Blob exists:", !!data);
            console.error("- Blob type:", data?.type);
            console.error("- Is image type:", data?.type?.startsWith("image"));
          }
        })
        .catch((err) => {
          console.error("💥 Fetch error occurred:");
          console.error("- Error message:", err.message);
          console.error("- Error object:", err);
          console.error("- Stack trace:", err.stack);
        })
      return
    }
    
    console.log("🔄 No image parameter, using default behavior...");
    
    // Comportement par défaut
    const headers = new Headers()
    headers.append("pragma", "no-cache")
    headers.append("cache-control", "no-cache")
    
    console.log("🔗 Fetching default input image from:", `${API_ENDPOINT}/inputimage`);

    fetch(`${API_ENDPOINT}/inputimage`, { headers })
      .then(async (res) => {
        console.log("📡 Default fetch response:");
        console.log("- Status:", res.status);
        console.log("- OK:", res.ok);
        
        if (!res.ok) {
          console.log("❌ Default response not OK");
          return
        }
        
        const filename = res.headers
          .get("content-disposition")
          ?.split("filename=")[1]
          .split(";")[0]
        
        console.log("📁 Default filename:", filename);

        const data = await res.blob()
        console.log("📦 Default blob:", data.size, "bytes, type:", data.type);
        
        if (data && data.type.startsWith("image")) {
          const userInput = new File(
            [data],
            filename !== undefined ? filename : "inputImage"
          )
          console.log("✅ Default image loaded successfully");
          setInputImage(userInput)
        }
      })
      .catch((err) => {
        console.error("💥 Default fetch error:", err)
      })
  }, [setInputImage])

  useEffect(() => {
    console.log("🎯 useInputImage useEffect triggered");
    fetchInputImage()
  }, [fetchInputImage])

  useEffect(() => {
    console.log("📊 Input image state changed:", inputImage ? `${inputImage.name} (${inputImage.size} bytes)` : "null");
  }, [inputImage])

  return inputImage
}