import { API_ENDPOINT } from "@/lib/api"
import { useCallback, useEffect, useState } from "react"

export default function useInputImage() {
  const [inputImage, setInputImage] = useState<File | null>(null)

  const fetchInputImage = useCallback(() => {
    // Vérifier si un paramètre ?image=... est présent dans l'URL
    const params = new URLSearchParams(window.location.search)
    const imageParam = params.get("image")
    if (imageParam) {
      // Charger l'image depuis le dossier de sortie
      fetch(`${API_ENDPOINT}/output_dir/${imageParam}`)
        .then(async (res) => {
          if (!res.ok) return
          const data = await res.blob()
          if (data && data.type.startsWith("image")) {
            const userInput = new File([data], imageParam)
            setInputImage(userInput)
          }
        })
        .catch((err) => {
          console.log(err)
        })
      return
    }
    // Comportement par défaut
    const headers = new Headers()
    headers.append("pragma", "no-cache")
    headers.append("cache-control", "no-cache")

    fetch(`${API_ENDPOINT}/inputimage`, { headers })
      .then(async (res) => {
        if (!res.ok) {
          return
        }
        const filename = res.headers
          .get("content-disposition")
          ?.split("filename=")[1]
          .split(";")[0]

        const data = await res.blob()
        if (data && data.type.startsWith("image")) {
          const userInput = new File(
            [data],
            filename !== undefined ? filename : "inputImage"
          )
          setInputImage(userInput)
        }
      })
      .catch((err) => {
        console.log(err)
      })
  }, [setInputImage])

  useEffect(() => {
    fetchInputImage()
  }, [fetchInputImage])

  return inputImage
}
