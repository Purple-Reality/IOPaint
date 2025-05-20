import { Link } from "react-router-dom"
import { HelpCircle } from "lucide-react"
import { PlayIcon } from "@radix-ui/react-icons"
import React, { useState } from "react"
import { IconButton, ImageUploadButton } from "@/components/ui/button"
import { useImage } from "@/hooks/useImage"

import { Popover, PopoverContent, PopoverTrigger } from "./ui/popover"
import PromptInput from "./PromptInput"
import { RotateCw, Upload } from "lucide-react"
import FileManager, { MASK_TAB } from "./FileManager"
import { getMediaBlob, getMediaFile } from "@/lib/api"
import { useStore, AppState } from "@/lib/states"
import { useToast } from "./ui/use-toast"
import {
  Dialog,
  DialogContent,
  DialogTrigger,
} from "./ui/dialog"
import { cn, fileToImage } from "@/lib/utils"

export const Header = () => {
  const [
    file,
    customMask,
    isInpainting,
    serverConfig,
    runMannually,
    enableUploadMask,
    model,
    setFile,
    setCustomFile,
    runInpainting,
    showPrevMask,
    hidePrevMask,
    imageHeight,
    imageWidth,
    handleFileManagerMaskSelect,
  ] = useStore((state: AppState) => [
    state.file,
    state.customMask,
    state.isInpainting,
    state.serverConfig,
    state.runMannually(),
    state.settings.enableUploadMask,
    state.settings.model,
    state.setFile,
    state.setCustomFile,
    state.runInpainting,
    state.showPrevMask,
    state.hidePrevMask,
    state.imageHeight,
    state.imageWidth,
    state.handleFileManagerMaskSelect,
  ])

  const { toast } = useToast()
  const [maskImage, maskImageLoaded] = useImage(customMask)
  const [openMaskPopover, setOpenMaskPopover] = useState(false)

  const handleRerunLastMask = () => {
    runInpainting()
  }

  const onRerunMouseEnter = () => {
    showPrevMask()
  }

  const onRerunMouseLeave = () => {
    hidePrevMask()
  }

  const handleOnPhotoClick = async (tab: string, filename: string) => {
    try {
      if (tab === MASK_TAB) {
        const maskBlob = await getMediaBlob(tab, filename)
        handleFileManagerMaskSelect(maskBlob)
      } else {
        const newFile = await getMediaFile(tab, filename)
        setFile(newFile)
      }
    } catch (e: any) {
      toast({
        variant: "destructive",
        description: e.message ? e.message : e.toString(),
      })
      return
    }
  }

  const isSettingsVisible = !serverConfig.disableModelSwitch;

  return (
    <header className="h-[60px] px-6 py-4 absolute top-[0] flex justify-between items-center w-full z-20 border-b backdrop-filter backdrop-blur-md bg-background/70">
      <div className="flex items-center gap-1">
        <Link to="/">
          <h1 className="text-lg font-bold">IOPaint</h1>
        </Link>

        {serverConfig.enableFileManager ? (
          <FileManager photoWidth={512} onPhotoClick={handleOnPhotoClick} />
        ) : (
          <></>
        )}

        <div
          className={cn([
            "flex items-center gap-1",
            file && enableUploadMask ? "visible" : "hidden",
          ])}
        >
          {customMask ? (
            <Popover open={openMaskPopover}>
              <PopoverTrigger
                className="btn-primary side-panel-trigger"
                onMouseEnter={() => setOpenMaskPopover(true)}
                onMouseLeave={() => setOpenMaskPopover(false)}
                style={{
                  visibility: customMask ? "visible" : "hidden",
                  outline: "none",
                }}
                onClick={() => {
                  if (customMask) {
                  }
                }}
              >
                <IconButton tooltip="Run custom mask">
                  <PlayIcon />
                </IconButton>
              </PopoverTrigger>
              <PopoverContent>
                {maskImageLoaded ? (
                  <img src={maskImage.src} alt="Custom mask" />
                ) : (
                  <></>
                )}
              </PopoverContent>
            </Popover>
          ) : (
            <></>
          )}
        </div>

        {file && !model.need_prompt ? (
          <IconButton
            disabled={isInpainting}
            tooltip="Rerun previous mask"
            onClick={handleRerunLastMask}
            onMouseEnter={onRerunMouseEnter}
            onMouseLeave={onRerunMouseLeave}
          >
            <RotateCw size={20} />
          </IconButton>
        ) : (
          <></>
        )}
      </div>

      {model.need_prompt ? <PromptInput /> : <></>}

      <div className="flex gap-1">
        {/* SettingsDialog masqué pour intégration Unity */}
        {/* {isSettingsVisible && <SettingsDialog />} */}
      </div>
    </header>
  )
}
