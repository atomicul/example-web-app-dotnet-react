import { CheckIcon, ChevronRightIcon } from 'lucide-react'
import {
  createContext,
  useContext,
  useState,
  type ReactElement,
  type ReactNode,
} from 'react'
import { Button } from '@/components/ui/button'
import {
  Drawer,
  DrawerContent,
  DrawerHeader,
  DrawerTitle,
  DrawerTrigger,
} from '@/components/ui/drawer'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
  DropdownMenuSeparator,
  DropdownMenuSub,
  DropdownMenuSubContent,
  DropdownMenuSubTrigger,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { Separator } from '@/components/ui/separator'
import { useIsMobile } from '@/hooks/use-mobile'

const MenuContext = createContext<{ isMobile: boolean; close: () => void }>({
  isMobile: false,
  close: () => {},
})

const RadioContext = createContext<{
  value?: string
  onValueChange?: (value: string) => void
}>({})

const drawerItem = 'h-11 w-full justify-start gap-3 px-3 text-base font-normal'

/**
 * A menu that drops down from its trigger on big screens and slides up from the bottom of the screen as a drawer
 * on phones, where each submenu is a drawer of its own. Picking an item closes the whole menu.
 */
export function Menu({ children }: { children: ReactNode }) {
  const isMobile = useIsMobile()
  const [open, setOpen] = useState(false)

  return (
    <MenuContext.Provider value={{ isMobile, close: () => setOpen(false) }}>
      {isMobile ? (
        <Drawer open={open} onOpenChange={setOpen} showSwipeHandle>
          {children}
        </Drawer>
      ) : (
        <DropdownMenu open={open} onOpenChange={setOpen}>
          {children}
        </DropdownMenu>
      )}
    </MenuContext.Provider>
  )
}

export function MenuTrigger({
  render,
  children,
}: {
  render: ReactElement
  children: ReactNode
}) {
  const { isMobile } = useContext(MenuContext)
  const Trigger = isMobile ? DrawerTrigger : DropdownMenuTrigger

  return <Trigger render={render}>{children}</Trigger>
}

/** The menu's items; `title` names the menu for assistive technology. */
export function MenuContent({
  title,
  align,
  children,
}: {
  title: string
  align?: 'start' | 'center' | 'end'
  children: ReactNode
}) {
  const { isMobile } = useContext(MenuContext)

  if (!isMobile) {
    return (
      <DropdownMenuContent aria-label={title} align={align} className="w-56">
        {children}
      </DropdownMenuContent>
    )
  }
  return (
    <DrawerItems title={title} hideTitle>
      {children}
    </DrawerItems>
  )
}

function DrawerItems({
  title,
  hideTitle,
  children,
}: {
  title: string
  hideTitle?: boolean
  children: ReactNode
}) {
  return (
    <DrawerContent>
      <DrawerHeader className={hideTitle ? 'sr-only' : undefined}>
        <DrawerTitle>{title}</DrawerTitle>
      </DrawerHeader>
      <div className="flex flex-col overflow-y-auto p-2 pb-[calc(--spacing(4)+env(safe-area-inset-bottom))]">
        {children}
      </div>
    </DrawerContent>
  )
}

export function MenuGroup({ children }: { children: ReactNode }) {
  const { isMobile } = useContext(MenuContext)

  if (!isMobile) {
    return <DropdownMenuGroup>{children}</DropdownMenuGroup>
  }
  return <div role="group">{children}</div>
}

export function MenuLabel({ children }: { children: ReactNode }) {
  const { isMobile } = useContext(MenuContext)

  if (!isMobile) {
    return <DropdownMenuLabel>{children}</DropdownMenuLabel>
  }
  return <div className="text-muted-foreground px-3 py-2">{children}</div>
}

export function MenuSeparator() {
  const { isMobile } = useContext(MenuContext)

  if (!isMobile) {
    return <DropdownMenuSeparator />
  }
  return <Separator className="my-2" />
}

export function MenuItem({
  onClick,
  disabled,
  children,
}: {
  onClick: () => void
  disabled?: boolean
  children: ReactNode
}) {
  const { isMobile, close } = useContext(MenuContext)

  if (!isMobile) {
    return (
      <DropdownMenuItem onClick={onClick} disabled={disabled}>
        {children}
      </DropdownMenuItem>
    )
  }

  function pick() {
    close()
    onClick()
  }

  return (
    <Button
      variant="ghost"
      className={drawerItem}
      disabled={disabled}
      onClick={pick}
    >
      {children}
    </Button>
  )
}

export function MenuSub({ children }: { children: ReactNode }) {
  const { isMobile } = useContext(MenuContext)

  if (!isMobile) {
    return <DropdownMenuSub>{children}</DropdownMenuSub>
  }
  return <Drawer showSwipeHandle>{children}</Drawer>
}

export function MenuSubTrigger({ children }: { children: ReactNode }) {
  const { isMobile } = useContext(MenuContext)

  if (!isMobile) {
    return <DropdownMenuSubTrigger>{children}</DropdownMenuSubTrigger>
  }
  return (
    <DrawerTrigger render={<Button variant="ghost" className={drawerItem} />}>
      {children}
      <ChevronRightIcon className="ml-auto" />
    </DrawerTrigger>
  )
}

/** The submenu's items; on phones, `title` heads the drawer they open in. */
export function MenuSubContent({
  title,
  children,
}: {
  title: string
  children: ReactNode
}) {
  const { isMobile } = useContext(MenuContext)

  if (!isMobile) {
    return <DropdownMenuSubContent>{children}</DropdownMenuSubContent>
  }
  return <DrawerItems title={title}>{children}</DrawerItems>
}

/** Items of which one is checked, the one whose value is `value`. */
export function MenuRadioGroup({
  value,
  onValueChange,
  children,
}: {
  value: string
  onValueChange?: (value: string) => void
  children: ReactNode
}) {
  const { isMobile } = useContext(MenuContext)

  if (!isMobile) {
    return (
      <DropdownMenuRadioGroup value={value} onValueChange={onValueChange}>
        {children}
      </DropdownMenuRadioGroup>
    )
  }
  return (
    <RadioContext.Provider value={{ value, onValueChange }}>
      <div role="group">{children}</div>
    </RadioContext.Provider>
  )
}

export function MenuRadioItem({
  value,
  disabled,
  children,
}: {
  value: string
  disabled?: boolean
  children: ReactNode
}) {
  const { isMobile, close } = useContext(MenuContext)
  const radio = useContext(RadioContext)

  if (!isMobile) {
    return (
      <DropdownMenuRadioItem value={value} disabled={disabled} closeOnClick>
        {children}
      </DropdownMenuRadioItem>
    )
  }

  const checked = radio.value === value

  function pick() {
    close()
    radio.onValueChange?.(value)
  }

  return (
    <Button
      variant="ghost"
      className={drawerItem}
      disabled={disabled}
      aria-pressed={checked}
      onClick={pick}
    >
      {children}
      {checked && <CheckIcon className="ml-auto" />}
    </Button>
  )
}
